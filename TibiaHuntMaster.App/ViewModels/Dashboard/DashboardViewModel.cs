using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services;
using TibiaHuntMaster.App.Services.Diagnostics;
using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.App.ViewModels.Analyzer;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;
using TibiaHuntMaster.Infrastructure.Services.System;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class NavigationItem : ObservableObject
    {
        public string Icon { get; }

        public string LocalizationKey { get; }

        public string DescriptionLocalizationKey { get; }

        public Thickness ContentMargin { get; }

        public Type? ParentViewModelType { get; }

        public Type ViewModelType { get; }

        public bool IsStandaloneMenuItem => ParentViewModelType == null && ViewModelType != typeof(OverviewViewModel);

        [ObservableProperty]private bool _isSelected;
        [ObservableProperty]private string _title = string.Empty;
        [ObservableProperty]private string _description = string.Empty;

        public NavigationItem(
            string icon,
            string localizationKey,
            string descriptionLocalizationKey,
            Type viewModelType,
            bool isSubItem = false,
            Type? parentViewModelType = null)
        {
            Icon = icon;
            LocalizationKey = localizationKey;
            DescriptionLocalizationKey = descriptionLocalizationKey;
            ViewModelType = viewModelType;
            ParentViewModelType = parentViewModelType;
            ContentMargin = isSubItem ? new Thickness(18, 0, 0, 0) : new Thickness(0);
        }
    }

    public sealed record LanguageItem(string Code, string DisplayName);

    public sealed partial class CharacterQuickSwitchItem : ObservableObject
    {
        private string _levelText;

        public CharacterQuickSwitchItem(Character character, string levelText)
        {
            Character = character;
            _levelText = levelText;
        }

        public Character Character { get; }

        public string Name => Character.Name;

        public string Vocation => Character.Vocation;

        public string LevelText
        {
            get => _levelText;
            private set => SetProperty(ref _levelText, value);
        }

        [ObservableProperty]private bool _isCurrent;

        public void UpdateLevelText(string levelText)
        {
            LevelText = levelText;
        }
    }

    public sealed partial class DashboardViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService = null!;
        private readonly IServiceProvider _services = null!;
        private readonly ICharacterService _characterService = null!;
        private readonly ThemeService _themeService = null!;
        private readonly IDiagnosticsService _diagnosticsService = null!;
        private readonly IFileRevealService _fileRevealService = null!;
        private readonly ILocalizationService _localizationService = null!;
        private readonly IContentService _contentService = null!;
        private readonly MonsterSpawnSeedService _monsterSpawnSeedService = null!;
        private readonly ImbuementSeedService _imbuementSeedService = null!;
        private readonly ILogger<DashboardViewModel> _logger = null!;
        private readonly NavigationItem _overviewNavigationItem;
        private readonly NavigationItem _progressNavigationItem;
        private readonly NavigationItem _economyNavigationItem;
        private readonly List<NavigationItem> _allMenuItems = [];

        [ObservableProperty]private Character? _activeCharacter;

        [ObservableProperty]private ViewModelBase? _currentContent;

        [ObservableProperty]private NavigationItem? _selectedNavigationItem;

        [ObservableProperty]private string _currentThemeIcon = "🌙";

        [ObservableProperty]private LanguageItem? _selectedLanguage;

        [ObservableProperty]private bool _isNavigationPaneOpen = true;
        [ObservableProperty]private bool _isExportingDiagnostics;
        [ObservableProperty]private bool _isCharacterSwitcherOpen;
        [ObservableProperty]private bool _isCharacterSwitcherLoading;
        [ObservableProperty]private bool _isSettingsDialogOpen;
        [ObservableProperty]private bool _isAddCharacterFormOpen;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UpdateDatabaseCommand))]
        [NotifyCanExecuteChangedFor(nameof(OpenRebuildDatabaseConfirmationCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConfirmRebuildDatabaseCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelRebuildDatabaseCommand))]
        private bool _isDatabaseMaintenanceRunning;
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ImportCharacterCommand))]
        private bool _isImportingCharacter;
        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ImportCharacterCommand))]
        private string _newCharacterName = string.Empty;
        [ObservableProperty]private string _characterSwitcherError = string.Empty;
        [ObservableProperty]private string _selectedSettingsTab = SettingsTabLanguage;
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasDiagnosticsStatus))]
        private string _diagnosticsStatusText = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDatabaseMaintenanceStatus))]
        private string _databaseMaintenanceStatusText = string.Empty;
        [ObservableProperty]private bool _isDatabaseRebuildConfirmationVisible;

        // Flag um automatische Navigation zu unterdrücken (für Back-Button Logik)
        private bool _suppressNavigationLogic;
        private bool _suppressLanguageSelectionChange;
        private const string SettingsTabLanguage = "language";
        private const string SettingsTabTheme = "theme";
        private const string SettingsTabSupport = "support";

        private readonly Dictionary<Type, Action> _menuNavigation;

        public DashboardViewModel(
            IServiceProvider services,
            INavigationService navigationService,
            ICharacterService characterService,
            ThemeService themeService,
            ILocalizationService localizationService,
            IDiagnosticsService diagnosticsService,
            IFileRevealService fileRevealService,
            IContentService contentService,
            MonsterSpawnSeedService monsterSpawnSeedService,
            ImbuementSeedService imbuementSeedService,
            ILogger<DashboardViewModel> logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(navigationService);
            ArgumentNullException.ThrowIfNull(characterService);
            ArgumentNullException.ThrowIfNull(themeService);
            ArgumentNullException.ThrowIfNull(localizationService);
            ArgumentNullException.ThrowIfNull(diagnosticsService);
            ArgumentNullException.ThrowIfNull(fileRevealService);
            ArgumentNullException.ThrowIfNull(contentService);
            ArgumentNullException.ThrowIfNull(monsterSpawnSeedService);
            ArgumentNullException.ThrowIfNull(imbuementSeedService);
            ArgumentNullException.ThrowIfNull(logger);

            _services = services;
            _navigationService = navigationService;
            _characterService = characterService;
            _themeService = themeService;
            _localizationService = localizationService;
            _diagnosticsService = diagnosticsService;
            _fileRevealService = fileRevealService;
            _contentService = contentService;
            _monsterSpawnSeedService = monsterSpawnSeedService;
            _imbuementSeedService = imbuementSeedService;
            _logger = logger;

            // Subscribe to navigation changes to update CurrentContent
            _navigationService.Navigated += OnNavigationChanged;

            // Subscribe to theme changes to update icon
            _themeService!.ThemeChanged += OnThemeChanged;
            UpdateThemeIcon(_themeService?.CurrentTheme ?? AppTheme.Dark);

            // Subscribe to language changes to update menu items
            _localizationService.PropertyChanged += OnLanguageChanged;

            // Initialize menu items with localization keys
            _overviewNavigationItem = new NavigationItem("🏠", "Nav_Overview", "NavDesc_Overview", typeof(OverviewViewModel));
            _progressNavigationItem = new NavigationItem("📈", "Nav_Progress", "NavDesc_Progress", typeof(ProgressViewModel), isSubItem: true, parentViewModelType: typeof(OverviewViewModel));
            _economyNavigationItem = new NavigationItem("💰", "Nav_Economy", "NavDesc_Economy", typeof(EconomyViewModel), isSubItem: true, parentViewModelType: typeof(OverviewViewModel));

            _allMenuItems.Add(_overviewNavigationItem);
            _allMenuItems.Add(_progressNavigationItem);
            _allMenuItems.Add(_economyNavigationItem);
            _allMenuItems.Add(new NavigationItem("⚔️", "Nav_HuntAnalyzer", "NavDesc_HuntAnalyzer", typeof(HuntAnalyzerViewModel)));
            _allMenuItems.Add(new NavigationItem("📜", "Nav_History", "NavDesc_History", typeof(HistoryViewModel)));
            _allMenuItems.Add(new NavigationItem("🎯", "Nav_GoalHistory", "NavDesc_GoalHistory", typeof(GoalHistoryViewModel)));
            _allMenuItems.Add(new NavigationItem("🗺️", "Nav_HuntingPlaces", "NavDesc_HuntingPlaces", typeof(HuntingPlacesViewModel)));
            _allMenuItems.Add(new NavigationItem("🧭", "Nav_Minimap", "NavDesc_Minimap", typeof(MinimapViewModel)));

            MenuItems.Add(_overviewNavigationItem);
            foreach(NavigationItem item in _allMenuItems.Where(item => item.ParentViewModelType == null && item != _overviewNavigationItem))
            {
                MenuItems.Add(item);
            }

            // Update menu titles with current language
            UpdateMenuTitles();

            // Initialize language options
            InitializeLanguages();

            _menuNavigation = CreateMenuNavigationMap();
            UpdateNavigationPresentation();
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update menu titles when language changes
            UpdateMenuTitles();

            // Refresh language list display names
            RefreshLanguageDisplayNames();
            OnPropertyChanged(nameof(NavigationPaneToggleToolTip));
            OnPropertyChanged(nameof(SettingsTitle));
            OnPropertyChanged(nameof(SwitchCharacterTitle));
            OnPropertyChanged(nameof(ThemeTabTitle));
            RefreshCharacterQuickSwitchItems();
        }

        private void UpdateMenuTitles()
        {
            foreach(NavigationItem item in _allMenuItems)
            {
                item.Title = _localizationService[item.LocalizationKey];
                item.Description = _localizationService[item.DescriptionLocalizationKey];
            }
        }

        private void RefreshLanguageDisplayNames()
        {
            var languageMap = new System.Collections.Generic.Dictionary<string, string>
            {
                {
                    "en", _localizationService["Language_English"]
                },
                {
                    "de", _localizationService["Language_German"]
                },
                {
                    "pl", _localizationService["Language_Polish"]
                },
                {
                    "es", _localizationService["Language_Spanish"]
                },
                {
                    "pt", _localizationService["Language_Portuguese"]
                },
                {
                    "sv", _localizationService["Language_Swedish"]
                }
            };

            LanguageItem? currentSelection = SelectedLanguage;
            AvailableLanguages.Clear();

            foreach(var lang in _localizationService.GetAvailableLanguages())
            {
                string displayName = languageMap.TryGetValue(lang, out string? name) ? name : lang.ToUpper();
                AvailableLanguages.Add(new LanguageItem(lang, displayName));
            }

            // Restore selection
            if(currentSelection != null)
            {
                SetSelectedLanguageSilently(AvailableLanguages.FirstOrDefault(l => l.Code == currentSelection.Code));
            }
        }

        private void OnThemeChanged(object? sender, AppTheme theme)
        {
            UpdateThemeIcon(theme);
            OnPropertyChanged(nameof(IsDarkThemeSelected));
            OnPropertyChanged(nameof(IsLightThemeSelected));
        }

        private void UpdateThemeIcon(AppTheme theme)
        {
            CurrentThemeIcon = theme == AppTheme.Dark ? "🌙" : "☀️";
        }

        public ObservableCollection<NavigationItem> MenuItems { get; } = new();

        public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new();

        public ObservableCollection<CharacterQuickSwitchItem> QuickSwitchCharacters { get; } = new();

        public NavigationItem OverviewNavigationItem => _overviewNavigationItem;

        public NavigationItem ProgressNavigationItem => _progressNavigationItem;

        public NavigationItem EconomyNavigationItem => _economyNavigationItem;

        public bool IsOverviewSectionExpanded => IsOverviewSectionActive(SelectedNavigationItem?.ViewModelType ?? CurrentContent?.GetType());

        public string OverviewSectionGlyph => IsOverviewSectionExpanded ? "▾" : "▸";

        public bool IsNavigationPaneCollapsed => !IsNavigationPaneOpen;

        public string NavigationPaneToggleGlyph => IsNavigationPaneOpen ? "◀" : "▶";

        public string NavigationPaneToggleToolTip => _localizationService[IsNavigationPaneOpen
            ? "Dashboard_CollapseMenu"
            : "Dashboard_ExpandMenu"];

        public bool HasDiagnosticsStatus => !string.IsNullOrWhiteSpace(DiagnosticsStatusText);

        public bool ShowDiagnosticsStatus => HasDiagnosticsStatus;

        public bool ShowDatabaseMaintenanceStatus => !string.IsNullOrWhiteSpace(DatabaseMaintenanceStatusText);

        public string SettingsTitle => _localizationService["Dashboard_Settings"];

        public string SwitchCharacterTitle => _localizationService["Dashboard_SwitchCharacter"];

        public string ThemeTabTitle => _localizationService["Settings_Theme"];

        public bool ShowAddCharacterButton => !IsAddCharacterFormOpen;

        public bool IsLanguageTabSelected => SelectedSettingsTab == SettingsTabLanguage;

        public bool IsThemeTabSelected => SelectedSettingsTab == SettingsTabTheme;

        public bool IsSupportTabSelected => SelectedSettingsTab == SettingsTabSupport;

        public bool IsDarkThemeSelected => _themeService.CurrentTheme == AppTheme.Dark;

        public bool IsLightThemeSelected => _themeService.CurrentTheme == AppTheme.Light;

        private void InitializeLanguages()
        {
            var languageMap = new System.Collections.Generic.Dictionary<string, string>
            {
                {
                    "en", _localizationService["Language_English"]
                },
                {
                    "de", _localizationService["Language_German"]
                },
                {
                    "pl", _localizationService["Language_Polish"]
                },
                {
                    "es", _localizationService["Language_Spanish"]
                },
                {
                    "pt", _localizationService["Language_Portuguese"]
                },
                {
                    "sv", _localizationService["Language_Swedish"]
                }
            };

            foreach(var lang in _localizationService.GetAvailableLanguages())
            {
                string displayName = languageMap.TryGetValue(lang, out string? name) ? name : lang.ToUpper();
                AvailableLanguages.Add(new LanguageItem(lang, displayName));
            }

            // Set current language
            string currentCode = _localizationService.CurrentCulture.TwoLetterISOLanguageName;
            SetSelectedLanguageSilently(AvailableLanguages.FirstOrDefault(l => l.Code == currentCode));
        }

        private void OnNavigationChanged(ViewModelBase viewModel)
        {
            CurrentContent = viewModel;

            // Sync the menu selection with the current view
            _suppressNavigationLogic = true;
            try
            {
                NavigationItem? matchingItem = _allMenuItems.FirstOrDefault(m => m.ViewModelType == viewModel.GetType());
                if(matchingItem != null)
                {
                    SelectedNavigationItem = matchingItem;
                }
            }
            finally
            {
                _suppressNavigationLogic = false;
            }

            UpdateNavigationPresentation();
        }

        // Automatische Navigation bei Menü-Klick
        partial void OnSelectedNavigationItemChanged(NavigationItem? value)
        {
            UpdateNavigationPresentation();

            if(_suppressNavigationLogic || value == null || ActiveCharacter == null)
            {
                return;
            }

            if (_menuNavigation.TryGetValue(value.ViewModelType, out Action? navigateAction))
            {
                navigateAction();
            }
        }

        partial void OnIsNavigationPaneOpenChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNavigationPaneCollapsed));
            OnPropertyChanged(nameof(NavigationPaneToggleGlyph));
            OnPropertyChanged(nameof(NavigationPaneToggleToolTip));
            OnPropertyChanged(nameof(ShowDiagnosticsStatus));
        }

        partial void OnSelectedSettingsTabChanged(string value)
        {
            OnPropertyChanged(nameof(IsLanguageTabSelected));
            OnPropertyChanged(nameof(IsThemeTabSelected));
            OnPropertyChanged(nameof(IsSupportTabSelected));
        }

        partial void OnIsAddCharacterFormOpenChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowAddCharacterButton));
        }

        partial void OnSelectedLanguageChanged(LanguageItem? value)
        {
            if(_suppressLanguageSelectionChange || value == null)
            {
                return;
            }

            if(_localizationService.CurrentCulture.TwoLetterISOLanguageName == value.Code)
            {
                return;
            }

            _localizationService.ChangeLanguage(value.Code);
        }

        partial void OnActiveCharacterChanged(Character? value)
        {
            RefreshCharacterQuickSwitchItems();
        }

        partial void OnDiagnosticsStatusTextChanged(string value)
        {
            OnPropertyChanged(nameof(ShowDiagnosticsStatus));
        }

        partial void OnDatabaseMaintenanceStatusTextChanged(string value)
        {
            OnPropertyChanged(nameof(ShowDatabaseMaintenanceStatus));
        }

        internal void Initialize(Character character)
        {
            ActiveCharacter = character;
            SelectedNavigationItem = OverviewNavigationItem;
            UpdateNavigationPresentation();
            RefreshCharacterQuickSwitchItems();
        }

        [RelayCommand]
        private void SelectNavigationItem(NavigationItem? item)
        {
            if(item == null)
            {
                return;
            }

            SelectedNavigationItem = item;
        }

        [RelayCommand]
        private void ToggleNavigationPane()
        {
            IsNavigationPaneOpen = !IsNavigationPaneOpen;
        }

        private void UpdateNavigationPresentation()
        {
            Type? activeViewModelType = SelectedNavigationItem?.ViewModelType ?? CurrentContent?.GetType();
            foreach(NavigationItem item in _allMenuItems)
            {
                item.IsSelected = activeViewModelType == item.ViewModelType;
            }

            OnPropertyChanged(nameof(IsOverviewSectionExpanded));
            OnPropertyChanged(nameof(OverviewSectionGlyph));
        }

        private bool IsOverviewSectionActive(Type? activeViewModelType)
        {
            if(activeViewModelType == null)
            {
                return false;
            }

            if(activeViewModelType == typeof(OverviewViewModel)
               || activeViewModelType == typeof(ProgressViewModel)
               || activeViewModelType == typeof(EconomyViewModel))
            {
                return true;
            }

            return false;
        }

        private Dictionary<Type, Action> CreateMenuNavigationMap()
        {
            Dictionary<Type, Action> map = new Dictionary<Type, Action>
            {
                [typeof(OverviewViewModel)] = NavigateToOverview,
                [typeof(ProgressViewModel)] = NavigateToProgress,
                [typeof(EconomyViewModel)] = NavigateToEconomy,
                [typeof(HuntAnalyzerViewModel)] = NavigateToAnalyzer,
                [typeof(HistoryViewModel)] = NavigateToHistory,
                [typeof(HuntingPlacesViewModel)] = NavigateToHuntingPlaces,
                [typeof(GoalHistoryViewModel)] = NavigateToGoalHistory,
                [typeof(MinimapViewModel)] = NavigateToMinimap,
            };

            return map;
        }

        private bool TryGetActiveCharacter(out Character activeCharacter)
        {
            Character? currentCharacter = ActiveCharacter;
            if(currentCharacter == null)
            {
                activeCharacter = null!;
                return false;
            }

            activeCharacter = currentCharacter;
            return true;
        }

        private void NavigateToOverview()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<OverviewViewModel>(new OverviewWithCharacter(activeCharacter));
        }

        private void NavigateToProgress()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<ProgressViewModel>(new ProgressWithCharacter(activeCharacter));
        }

        private void NavigateToEconomy()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<EconomyViewModel>(new EconomyWithCharacter(activeCharacter));
        }

        private void NavigateToAnalyzer()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<HuntAnalyzerViewModel>(new AnalyzerWithCharacter(activeCharacter.Name));
        }

        private void NavigateToHistory()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<HistoryViewModel>(new HistoryWithCharacter(activeCharacter.Name));
        }

        private void NavigateToHuntingPlaces()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<HuntingPlacesViewModel>(new HuntingPlacesWithCharacter(activeCharacter));
        }

        private void NavigateToGoalHistory()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<GoalHistoryViewModel>(
                new GoalHistoryWithCharacter(activeCharacter.Name, activeCharacter.Id, activeCharacter.Level));
        }

        private void NavigateToMinimap()
        {
            if(!TryGetActiveCharacter(out Character activeCharacter))
            {
                return;
            }

            _navigationService.NavigateTo<MinimapViewModel>(new MinimapWithCharacter(activeCharacter));
        }

        [RelayCommand]
        private async Task ExportDiagnosticsAsync()
        {
            if(IsExportingDiagnostics)
            {
                return;
            }

            IsExportingDiagnostics = true;
            DiagnosticsStatusText = _localizationService["Dashboard_DiagnosticsExporting"];

            try
            {
                DiagnosticsExportResult result = await _diagnosticsService.ExportDiagnosticsAsync();
                await _fileRevealService.RevealFileAsync(result.ArchivePath);
                DiagnosticsStatusText = _localizationService["Dashboard_DiagnosticsExportOpened"];
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to export diagnostics archive.");
                DiagnosticsStatusText = _localizationService["Dashboard_DiagnosticsExportFailed"];
            }
            finally
            {
                IsExportingDiagnostics = false;
            }
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        [RelayCommand]
        private void SelectLanguage(LanguageItem? language)
        {
            if(language == null)
            {
                return;
            }

            _localizationService.ChangeLanguage(language.Code);
            SetSelectedLanguageSilently(language);
        }

        [RelayCommand]
        private void OpenSettings()
        {
            SelectedSettingsTab = SettingsTabLanguage;
            IsDatabaseRebuildConfirmationVisible = false;
            IsSettingsDialogOpen = true;
        }

        [RelayCommand]
        private void CloseSettings()
        {
            IsDatabaseRebuildConfirmationVisible = false;
            IsSettingsDialogOpen = false;
        }

        [RelayCommand]
        private void SelectSettingsTab(string? tab)
        {
            if(string.IsNullOrWhiteSpace(tab))
            {
                return;
            }

            SelectedSettingsTab = tab;
        }

        private bool CanRunDatabaseMaintenance()
        {
            return !IsDatabaseMaintenanceRunning;
        }

        [RelayCommand(CanExecute = nameof(CanRunDatabaseMaintenance))]
        private async Task UpdateDatabaseAsync()
        {
            await RunDatabaseMaintenanceAsync(rebuildContent: false);
        }

        [RelayCommand(CanExecute = nameof(CanRunDatabaseMaintenance))]
        private void OpenRebuildDatabaseConfirmation()
        {
            DatabaseMaintenanceStatusText = string.Empty;
            IsDatabaseRebuildConfirmationVisible = true;
        }

        [RelayCommand(CanExecute = nameof(CanRunDatabaseMaintenance))]
        private void CancelRebuildDatabase()
        {
            IsDatabaseRebuildConfirmationVisible = false;
        }

        [RelayCommand(CanExecute = nameof(CanRunDatabaseMaintenance))]
        private async Task ConfirmRebuildDatabaseAsync()
        {
            await RunDatabaseMaintenanceAsync(rebuildContent: true);
        }

        [RelayCommand]
        private async Task OpenCharacterSwitcherAsync()
        {
            if(IsNavigationPaneCollapsed)
            {
                IsNavigationPaneOpen = true;
            }

            if(IsCharacterSwitcherOpen)
            {
                CloseCharacterSwitcher();
                return;
            }

            CharacterSwitcherError = string.Empty;
            IsAddCharacterFormOpen = false;
            IsCharacterSwitcherOpen = true;
            await LoadQuickSwitchCharactersAsync();
        }

        [RelayCommand]
        private void CloseCharacterSwitcher()
        {
            IsCharacterSwitcherOpen = false;
            IsAddCharacterFormOpen = false;
            CharacterSwitcherError = string.Empty;
            NewCharacterName = string.Empty;
        }

        [RelayCommand]
        private void OpenAddCharacterForm()
        {
            IsAddCharacterFormOpen = true;
            CharacterSwitcherError = string.Empty;
            NewCharacterName = string.Empty;
        }

        [RelayCommand]
        private void CancelAddCharacter()
        {
            IsAddCharacterFormOpen = false;
            CharacterSwitcherError = string.Empty;
            NewCharacterName = string.Empty;
        }

        [RelayCommand]
        private async Task ActivateCharacterAsync(CharacterQuickSwitchItem? item)
        {
            if(item == null)
            {
                return;
            }

            if(ActiveCharacter?.Id == item.Character.Id)
            {
                NewCharacterName = string.Empty;
                IsCharacterSwitcherOpen = false;
                IsAddCharacterFormOpen = false;
                return;
            }

            ActiveCharacter = item.Character;
            CharacterActivated?.Invoke(item.Character);
            IsCharacterSwitcherOpen = false;
            IsAddCharacterFormOpen = false;
            CharacterSwitcherError = string.Empty;
            NewCharacterName = string.Empty;

            Type targetViewModelType = SelectedNavigationItem?.ViewModelType ?? typeof(OverviewViewModel);
            if(_menuNavigation.TryGetValue(targetViewModelType, out Action? navigateAction))
            {
                navigateAction();
            }
        }

        [RelayCommand(CanExecute = nameof(CanImportCharacter))]
        private async Task ImportCharacterAsync()
        {
            string requestedName = UserInputSanitizer.TrimAndTruncate(NewCharacterName, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(requestedName))
            {
                return;
            }

            if(UserInputSanitizer.ExceedsLength(NewCharacterName, UserInputLimits.CharacterNameMaxLength))
            {
                CharacterSwitcherError = $"Character name is too long (max {UserInputLimits.CharacterNameMaxLength} characters).";
                return;
            }

            IsImportingCharacter = true;
            CharacterSwitcherError = string.Empty;

            try
            {
                Character importedCharacter = await _characterService.ImportFromTibiaDataAsync(requestedName);
                await _characterService.SaveAsync(importedCharacter);

                await LoadQuickSwitchCharactersAsync();

                CharacterQuickSwitchItem? matchingItem = QuickSwitchCharacters.FirstOrDefault(item =>
                    item.Character.Name.Equals(importedCharacter.Name, StringComparison.OrdinalIgnoreCase));

                if(matchingItem != null)
                {
                    await ActivateCharacterAsync(matchingItem);
                }
                else
                {
                    IsAddCharacterFormOpen = false;
                    NewCharacterName = string.Empty;
                }
            }
            catch(Exception ex)
            {
                CharacterSwitcherError = string.Format(_localizationService["CharSelection_Error"], ex.Message);
            }
            finally
            {
                IsImportingCharacter = false;
            }
        }

        [RelayCommand]
        private void SetTheme(string? themeKey)
        {
            AppTheme targetTheme = string.Equals(themeKey, nameof(AppTheme.Light), StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Light
                : AppTheme.Dark;

            if(_themeService.CurrentTheme != targetTheme)
            {
                _themeService.SetTheme(targetTheme);
            }
        }

        private bool CanImportCharacter()
        {
            return !IsImportingCharacter && !string.IsNullOrWhiteSpace(NewCharacterName);
        }

        private async Task LoadQuickSwitchCharactersAsync()
        {
            IsCharacterSwitcherLoading = true;
            try
            {
                IReadOnlyList<Character> characters = await _characterService.ListAsync();
                QuickSwitchCharacters.Clear();

                foreach(Character character in characters)
                {
                    QuickSwitchCharacters.Add(new CharacterQuickSwitchItem(character, FormatLevelText(character.Level))
                    {
                        IsCurrent = ActiveCharacter?.Id == character.Id
                    });
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to load character quick switch list.");
                CharacterSwitcherError = string.Format(_localizationService["CharSelection_Error"], ex.Message);
            }
            finally
            {
                IsCharacterSwitcherLoading = false;
            }
        }

        private void RefreshCharacterQuickSwitchItems()
        {
            foreach(CharacterQuickSwitchItem item in QuickSwitchCharacters)
            {
                item.IsCurrent = ActiveCharacter?.Id == item.Character.Id;
                item.UpdateLevelText(FormatLevelText(item.Character.Level));
            }
        }

        private string FormatLevelText(int level)
        {
            return string.Format(_localizationService["CharSelection_Level"], level);
        }

        private void SetSelectedLanguageSilently(LanguageItem? language)
        {
            _suppressLanguageSelectionChange = true;
            try
            {
                SelectedLanguage = language;
            }
            finally
            {
                _suppressLanguageSelectionChange = false;
            }
        }

        private async Task RunDatabaseMaintenanceAsync(bool rebuildContent)
        {
            if(IsDatabaseMaintenanceRunning)
            {
                return;
            }

            IsDatabaseMaintenanceRunning = true;
            IsDatabaseRebuildConfirmationVisible = false;

            try
            {
                DatabaseMaintenanceStatusText = _localizationService["Settings_DatabasePreparing"];
                await _monsterSpawnSeedService.EnsureSpawnsSeededAsync();

                if(rebuildContent)
                {
                    DatabaseMaintenanceStatusText = _localizationService["Settings_DatabaseRebuilding"];
                    ContentReInitializationResult result = await _contentService.ReInitializeContentAsync();
                    DatabaseMaintenanceStatusText = _localizationService["Settings_DatabaseFinalizing"];
                    await _monsterSpawnSeedService.EnsureSpawnsSeededAsync();
                    await _imbuementSeedService.EnsureRecipesSeededAsync();
                    DatabaseMaintenanceStatusText = FormatDatabaseRebuildSummary(result);
                }
                else
                {
                    DatabaseMaintenanceStatusText = _localizationService["Settings_DatabaseUpdating"];
                    ContentRefreshResult result = await _contentService.RefreshContentAsync();
                    DatabaseMaintenanceStatusText = _localizationService["Settings_DatabaseFinalizing"];
                    await _monsterSpawnSeedService.EnsureSpawnsSeededAsync();
                    await _imbuementSeedService.EnsureRecipesSeededAsync();
                    DatabaseMaintenanceStatusText = FormatDatabaseRefreshSummary(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database maintenance action failed. Rebuild={RebuildContent}", rebuildContent);
                DatabaseMaintenanceStatusText = string.Format(
                    _localizationService["Settings_DatabaseMaintenanceFailed"],
                    ex.Message);
            }
            finally
            {
                IsDatabaseMaintenanceRunning = false;
            }
        }

        private string FormatDatabaseRefreshSummary(ContentRefreshResult result)
        {
            ContentOperationResult total = result.Total;
            return string.Format(
                _localizationService["Settings_DatabaseRefreshCompleted"],
                total.Loaded,
                total.Created,
                total.Updated,
                total.Skipped,
                total.Failed);
        }

        private string FormatDatabaseRebuildSummary(ContentReInitializationResult result)
        {
            ContentOperationResult total = result.Total;
            return string.Format(
                _localizationService["Settings_DatabaseRebuildCompleted"],
                total.Loaded,
                total.Created,
                total.Updated,
                total.Skipped,
                total.Failed);
        }

        public event Action<Character>? CharacterActivated;
    }
}
