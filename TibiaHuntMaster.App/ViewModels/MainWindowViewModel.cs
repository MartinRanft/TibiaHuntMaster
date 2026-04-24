using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.ViewModels.Dashboard;
using TibiaHuntMaster.App.ViewModels.Selection;
using TibiaHuntMaster.App.ViewModels.Startup;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;
using TibiaHuntMaster.Infrastructure.Services.Hunts;
using TibiaHuntMaster.Infrastructure.Services.System;

namespace TibiaHuntMaster.App.ViewModels
{
    public sealed partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly IHuntSessionService _huntSessionService;
        private readonly ImbuementSeedService _imbuementSeeder;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IServiceProvider _services;
        private readonly BackgroundSyncWorker _syncWorker; // <--- NEU

        [ObservableProperty]private ViewModelBase? _currentView;

        // Steuert, ob wir noch im Setup sind (für Banner-Unterdrückung)
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
        private bool _isSetupActive = true;

        [ObservableProperty]private string _windowTitle;

        public MainWindowViewModel(
            IServiceProvider services,
            ICharacterService characterService,
            IHuntSessionService huntSessionService,
            ImbuementSeedService imbuementSeeder,
            DataStatusService statusService,
            BackgroundSyncWorker syncWorker,
            ILocalizationService localizationService,
            ILogger<MainWindowViewModel> logger)
        {
            _services = services;
            _characterService = characterService;
            _huntSessionService = huntSessionService;
            _imbuementSeeder = imbuementSeeder;
            _logger = logger;
            StatusService = statusService;
            _syncWorker = syncWorker;
            _localizationService = localizationService;
            _windowTitle = _localizationService["Window_TitleDefault"];

            // Wir abonnieren Änderungen im StatusService, um IsBannerVisible zu aktualisieren
            StatusService.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName == nameof(DataStatusService.StatusMessage))
                {
                    OnPropertyChanged(nameof(IsBannerVisible));
                }
            };

            StartApplicationFlowSafe();
        }

        // Designer Constructor
        public MainWindowViewModel()
        {
            _services = null!;
            _characterService = null!;
            _huntSessionService = null!;
            _imbuementSeeder = null!;
            _logger = null!;
            StatusService = null!;
            _syncWorker = null!;
            _localizationService = null!;
            _windowTitle = "TibiaHuntMaster";
        }

        // Status Service für Banner
        public DataStatusService StatusService { get; }

        // Computed Property für die View
        public bool IsBannerVisible => !IsSetupActive && !string.IsNullOrEmpty(StatusService.StatusMessage);

        private void StartApplicationFlowSafe()
        {
            RunSafeFireAndForget(StartApplicationFlowAsync(), nameof(StartApplicationFlowAsync));
        }

        private void RunSafeFireAndForget(Task task, string operationName)
        {
            _ = ObserveTaskAsync(task, operationName);
        }

        private async Task ObserveTaskAsync(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OperationName} failed.", operationName);
            }
        }

        private async Task StartApplicationFlowAsync()
        {
            if(_services == null)
            {
                return;
            }

            SetupViewModel setupVm = _services.GetRequiredService<SetupViewModel>();
            CurrentView = setupVm;
            IsSetupActive = true;

            // 1. Initialer Blockierender Check (Setup View)
            bool setupSucceeded = await PerformInitialSetup(setupVm);
            if(!setupSucceeded)
            {
                return;
            }

            // 2. Navigation zur App
            await CheckForCharactersAndNavigate();

            // 3. Setup ist vorbei -> Banner darf jetzt erscheinen
            IsSetupActive = false;

            // 4. Jetzt den Background Worker starten (für Updates oder Retries)
            StartBackgroundWorker();

            // 5. Bestehende Sessions ohne HuntingPlaceId im Hintergrund nachverlinken.
            RunSafeFireAndForget(BackfillHistoricHuntingPlaceLinksAsync(), nameof(BackfillHistoricHuntingPlaceLinksAsync));
        }

        private async Task<bool> PerformInitialSetup(SetupViewModel vm)
        {
            vm.StatusMessage = _localizationService["Setup_CheckingDatabase"];
            vm.IsProgressIndeterminate = true;
            vm.ProgressValue = 10;
            vm.CurrentStep = _localizationService["Setup_CurrentStepDefault"];
            vm.ClearActivities();

            Action<bool, bool, bool, string>? previousStatusHandler = _syncWorker.OnStateChanged;
            IContentProgressService contentProgressService = _services.GetRequiredService<IContentProgressService>();

            void HandleProgress(ContentProgressUpdate update)
            {
                Dispatcher.UIThread.Post(() => vm.ApplyProgress(update));
            }

            contentProgressService.Reset();
            contentProgressService.ProgressChanged += HandleProgress;
            _syncWorker.OnStateChanged = (_, isSyncing, isRetry, message) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if(isRetry && !string.IsNullOrWhiteSpace(message))
                    {
                        vm.HintMessage = message;
                    }

                    vm.IsProgressIndeterminate = isSyncing || isRetry;
                });
            };

            try
            {
                vm.ProgressValue = 25;

                vm.StatusMessage = _localizationService["Setup_InitializingDatabase"];
                vm.ProgressValue = 35;
                await _syncWorker.RunOnceAsync();

                bool contentReady = await IsContentReadyAsync();
                if(!contentReady)
                {
                    vm.IsProgressIndeterminate = false;
                    vm.StatusMessage = _localizationService["Setup_ServerUnreachable"];
                    _logger.LogError("Initial setup finished without required content data in the database.");
                    return false;
                }

                vm.ProgressValue = 70;

                // Imbuement Rezepte anlegen (erstellt Dummys falls Items fehlen)
                vm.StatusMessage = _localizationService["Setup_PreparingConfig"];
                vm.ProgressValue = 85;
                await _imbuementSeeder.EnsureRecipesSeededAsync();

                vm.StatusMessage = _localizationService["Setup_Ready"];
                vm.IsProgressIndeterminate = false;
                vm.ProgressValue = 100;
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                // Kritischer DB Fehler (z.B. Disk voll, Permissions)
                _logger.LogError(ex, "PerformInitialSetup failed.");
                vm.IsProgressIndeterminate = false;
                vm.StatusMessage = string.Format(_localizationService["Setup_CriticalError"], ex.Message);
                return false;
            }
            finally
            {
                contentProgressService.ProgressChanged -= HandleProgress;
                _syncWorker.OnStateChanged = previousStatusHandler;
            }
        }

        private async Task<bool> IsContentReadyAsync()
        {
            IDbContextFactory<AppDbContext> dbFactory = _services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            bool hasItems = await db.Items.AnyAsync();
            bool hasCreatures = await db.Creatures.AnyAsync();
            bool hasHuntingPlaces = await db.HuntingPlaces.AnyAsync();

            return hasItems && hasCreatures && hasHuntingPlaces;
        }

        private void StartBackgroundWorker()
        {
            // Bridge: Worker -> StatusService -> UI
            _syncWorker.OnStateChanged = (isCritical, isSyncing, isRetry, msg) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    StatusService.IsCriticalMissing = isCritical;
                    StatusService.IsSyncing = isSyncing;
                    StatusService.IsInRetryDelay = isRetry;
                    StatusService.StatusMessage = msg;
                });
            };

            _syncWorker.Start();
        }

        private async Task BackfillHistoricHuntingPlaceLinksAsync()
        {
            try
            {
                int updated = await _huntSessionService.BackfillMissingHuntingPlaceLinksAsync();
                _logger.LogInformation("Backfill HuntSession.HuntingPlaceId updated {UpdatedCount} sessions.", updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backfill HuntSession.HuntingPlaceId failed.");
            }
        }

        [RelayCommand]
        private void CancelSync()
        {
            _syncWorker?.Cancel();
        }

        // ... CheckForCharactersAndNavigate etc. bleiben gleich ...

        private async Task CheckForCharactersAndNavigate()
        {
            try
            {
                await _characterService.ListAsync();
                NavigateToCharacterSelection();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check characters and navigate.");
            }
        }

        private void NavigateToCharacterSelection()
        {
            CharacterSelectionViewModel selectionVm = _services.GetRequiredService<CharacterSelectionViewModel>();
            selectionVm.CharacterSelected += OnCharacterSelected;
            CurrentView = selectionVm;
            WindowTitle = _localizationService["Window_TitleSelectCharacter"];
            RunSafeFireAndForget(selectionVm.LoadCharactersAsync(), "LoadCharactersAsync");
        }

        private void OnCharacterSelected(Character character)
        {
            NavigateToDashboard(character);
        }

        private void NavigateToDashboard(Character character)
        {
            DashboardViewModel dashboardVm = _services.GetRequiredService<DashboardViewModel>();
            dashboardVm.Initialize(character);
            dashboardVm.CharacterActivated += UpdateDashboardWindowTitle;
            CurrentView = dashboardVm;
            UpdateDashboardWindowTitle(character);
        }

        private void UpdateDashboardWindowTitle(Character character)
        {
            WindowTitle = string.Format(_localizationService["Window_TitleDashboard"], character.Name, character.Level, character.Vocation);
        }
    }
}
