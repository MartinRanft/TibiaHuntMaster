using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Map;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Core.Services;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Hunts;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed record DeathDisplayItem(string TimeAgo, string Reason, string KillerName, string ImageSource);

    public enum GoalDialogMode
    {
        None,
        Create,
        Edit,
        AddProgress
    }

    public sealed partial class OverviewViewModel : ViewModelBase, INavigationAware
    {
        private readonly ICharacterService _characterService;
        private readonly IGoalService _goalService;
        private readonly IHuntSessionService _huntService;
        private readonly ILocalizationService _localizationService;
        private readonly IMonsterImageCatalogService _monsterImageCatalogService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<OverviewViewModel> _logger;
        [ObservableProperty]private ObservableCollection<GoalItemViewModel> _activeGoals = [];
        [ObservableProperty]private long _avgXpPerHour;
        [ObservableProperty]private string _celebrationTitle = string.Empty;

        [ObservableProperty]private Character? _character;
        [ObservableProperty]private ObservableCollection<GoalItemViewModel> _completedGoals = [];
        [ObservableProperty]private GoalDialogMode _dialogMode = GoalDialogMode.None;
        private CharacterGoalEntity? _editingGoalEntity;

        private int _editingGoalId;

        // --- GOAL SYSTEM ---
        [ObservableProperty]private ObservableCollection<GoalItemViewModel> _goals = [];
        [ObservableProperty]private DateTimeOffset _goalStartDate = DateTimeOffset.Now;
        [ObservableProperty]private long _goalTargetInput;
        [ObservableProperty]private long _goalBaseLevelInput;

        [ObservableProperty]private string _goalTitleInput = string.Empty;

        // Dialog Steuerung
        [ObservableProperty]private bool _isDialogVisible;
        [ObservableProperty]private bool _isLoading;
        [ObservableProperty]private string _validationError = string.Empty;
        [ObservableProperty]private double _levelProgressPercent;

        [ObservableProperty]private int _newGoalTypeIndex;
        [ObservableProperty]private bool _isEditingLevelGoal;
        [ObservableProperty]private ObservableCollection<string> _goalTypeOptions = [];

        [ObservableProperty]private long _progressAddInput;
        [ObservableProperty]private string _progressUnitLabel = "";
        [ObservableProperty]private ObservableCollection<DeathDisplayItem> _recentDeaths = [];

        // Animation
        [ObservableProperty]private bool _showCelebration;
        [ObservableProperty]private long _totalBalance;
        [ObservableProperty]private TimeSpan _totalPlaytime;
        [ObservableProperty]private long _xpToNextLevel;

        public OverviewViewModel(
            IHuntSessionService huntService,
            ICharacterService characterService,
            IGoalService goalService,
            INavigationService navigationService,
            ILocalizationService localizationService,
            IMonsterImageCatalogService monsterImageCatalogService,
            ILogger<OverviewViewModel> logger)
        {
            _huntService = huntService;
            _characterService = characterService;
            _goalService = goalService;
            _navigationService = navigationService;
            _localizationService = localizationService;
            _monsterImageCatalogService = monsterImageCatalogService;
            _logger = logger;
            UpdateGoalTypeOptions();

            // Subscribe to language changes
            _localizationService.PropertyChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh deaths display when language changes
            if (Character != null)
            {
                RunSafeFireAndForget(PrepareDeaths(Character), nameof(PrepareDeaths));
            }

            UpdateGoalTypeOptions();
            UpdateProgressUnitLabel();
            OnPropertyChanged(nameof(EditTargetLabel));
        }

        // Designer Ctor
        public OverviewViewModel()
        {
            _huntService = null!;
            _characterService = null!;
            _goalService = null!;
            _navigationService = null!;
            _localizationService = null!;
            _monsterImageCatalogService = null!;
            _logger = null!;
            _goalTypeOptions =
            [
                "Level Goal",
                "Gold Goal"
            ];
        }

        // UI Properties
        public bool IsCreateMode => DialogMode == GoalDialogMode.Create;

        public bool IsEditMode => DialogMode == GoalDialogMode.Edit;

        public bool IsProgressMode => DialogMode == GoalDialogMode.AddProgress;

        public bool IsGoldGoalSelected => NewGoalTypeIndex == 1;
        public bool IsLevelGoalSelected => NewGoalTypeIndex == 0;
        public string EditTargetLabel => IsEditingLevelGoal
            ? _localizationService["Goal_TargetLevel"]
            : _localizationService["Goal_TargetValue"];

        private void UpdateGoalTypeOptions()
        {
            GoalTypeOptions.Clear();
            GoalTypeOptions.Add(_localizationService["Goal_LevelGoal"]);
            GoalTypeOptions.Add(_localizationService["Goal_GoldGoal"]);

            if(NewGoalTypeIndex < 0 || NewGoalTypeIndex >= GoalTypeOptions.Count)
            {
                NewGoalTypeIndex = 0;
            }
        }

        private void UpdateProgressUnitLabel()
        {
            if(DialogMode != GoalDialogMode.AddProgress || _editingGoalEntity == null)
            {
                return;
            }

            ProgressUnitLabel = _editingGoalEntity.Type == GoalType.Gold
                ? _localizationService["Goal_UnitGold"]
                : _localizationService["Goal_UnitLevels"];
        }

        // INavigationAware Implementation
        public void OnNavigatedTo(object? parameter)
        {
            if(parameter is OverviewWithCharacter overviewParam)
            {
                RunSafeFireAndForget(LoadDataAsync(overviewParam.Character), nameof(LoadDataAsync));
            }
        }

        private static void RunSafeFireAndForget(Task task, string operationName)
        {
            _ = ObserveTaskAsync(task, operationName);
        }

        private static async Task ObserveTaskAsync(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(OverviewViewModel)}] {operationName} failed: {ex}");
            }
        }

        public void OnNavigatedFrom()
        {
            // Cleanup if needed
        }

        partial void OnDialogModeChanged(GoalDialogMode value)
        {
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsProgressMode));
        }

        partial void OnNewGoalTypeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsGoldGoalSelected));
            OnPropertyChanged(nameof(IsLevelGoalSelected));

            if(value == 0 && Character != null && GoalBaseLevelInput <= 0)
            {
                GoalBaseLevelInput = Character.Level;
            }
        }

        partial void OnCharacterChanged(Character? value)
        {
            if(value == null)
            {
                return;
            }

            if(!IsDialogVisible || DialogMode == GoalDialogMode.Create)
            {
                GoalBaseLevelInput = value.Level;
            }
        }

        partial void OnIsEditingLevelGoalChanged(bool value)
        {
            OnPropertyChanged(nameof(EditTargetLabel));
        }

        internal async Task LoadDataAsync(Character lightweightCharacter)
        {
            if(lightweightCharacter == null)
            {
                return;
            }

            IsLoading = true;
            try
            {
                Character? fullCharacter = await _characterService.GetByNameAsync(lightweightCharacter.Name);
                Character = fullCharacter ?? lightweightCharacter;

                long currentTotalXp = TibiaMathService.ExperienceForLevel(Character.Level);
                long xpForNextLevel = TibiaMathService.ExperienceForLevel(Character.Level + 1);
                XpToNextLevel = xpForNextLevel - currentTotalXp;
                LevelProgressPercent = 0;

                HuntStatistics? stats = await _huntService.GetStatisticsAsync(Character.Name, 1000);
                if(stats != null)
                {
                    TotalBalance = stats.TotalProfit;
                    AvgXpPerHour = stats.AvgXpPerHour;
                    TotalPlaytime = stats.AvgDuration * stats.SessionCount;
                }
                else
                {
                    TotalBalance = 0;
                    AvgXpPerHour = 0;
                    TotalPlaytime = TimeSpan.Zero;
                }

                await PrepareDeaths(Character);
                await LoadGoals();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR LOAD DATA: {ex}");
                Character = lightweightCharacter;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadGoals()
        {
            if(Character == null)
            {
                return;
            }
            if(_goalService == null)
            {
                return;
            }

            List<GoalProgressResult> goalResults = await _goalService.GetGoalsForCharacterAsync(Character.Id, Character.Level);

            // Celebration Logic (Check for new completions)
            HashSet<int> previouslyCompleted = CompletedGoals.Select(g => g.Entity.Id).ToHashSet();

            ActiveGoals.Clear();
            CompletedGoals.Clear();
            // Legacy List for safety if view binds to Goals
            Goals.Clear();

            foreach(GoalProgressResult res in goalResults)
            {
                GoalItemViewModel vm = new(res);
                Goals.Add(vm); // Add to master list

                if(res.Goal.IsCompleted)
                {
                    CompletedGoals.Add(vm);
                    if(!previouslyCompleted.Contains(res.Goal.Id) && previouslyCompleted.Count > 0)
                    {
                        RunSafeFireAndForget(TriggerCelebrationAsync(res.Goal.Title), nameof(TriggerCelebrationAsync));
                    }
                }
                else
                {
                    ActiveGoals.Add(vm);
                }
            }
        }

        private async Task TriggerCelebrationAsync(string title)
        {
            CelebrationTitle = title;
            ShowCelebration = true;
            await Task.Delay(4000);
            ShowCelebration = false;
        }

        [RelayCommand]
        private void NavigateToHistory(GoalItemViewModel vm)
        {
            if(Character == null)
            {
                return;
            }

            // Use NavigationService instead of events
            _navigationService.NavigateTo<HistoryViewModel>(
                new HistoryWithGoalFilter(Character.Name, vm.Entity)
            );
        }

        // --- DIALOG COMMANDS ---

        // FIX: Name muss OpenAddGoal sein, damit OpenAddGoalCommand generiert wird!
        [RelayCommand]
        public void OpenAddGoal()
        {
            DialogMode = GoalDialogMode.Create;
            GoalTitleInput = "";
            GoalTargetInput = 0;
            NewGoalTypeIndex = 1;
            GoalBaseLevelInput = Character?.Level ?? 0;
            IsEditingLevelGoal = false;
            GoalStartDate = DateTimeOffset.Now;

            IsDialogVisible = true;
        }

        [RelayCommand]
        public void OpenEditDialog(GoalItemViewModel vm)
        {
            _editingGoalId = vm.Entity.Id;
            _editingGoalEntity = vm.Entity;

            DialogMode = GoalDialogMode.Edit;
            GoalTitleInput = vm.Entity.Title;
            GoalTargetInput = vm.Entity.TargetValue;
            IsEditingLevelGoal = vm.Entity.Type == GoalType.Level;

            IsDialogVisible = true;
        }

        [RelayCommand]
        public void OpenAddProgressDialog(GoalItemViewModel vm)
        {
            _editingGoalId = vm.Entity.Id;
            _editingGoalEntity = vm.Entity;

            DialogMode = GoalDialogMode.AddProgress;
            ProgressAddInput = 0;
            ProgressUnitLabel = vm.Entity.Type == GoalType.Gold ? _localizationService["Goal_UnitGold"] : _localizationService["Goal_UnitLevels"];
            IsEditingLevelGoal = false;

            IsDialogVisible = true;
        }

        [RelayCommand]
        public void CloseDialog()
        {
            IsDialogVisible = false;
            DialogMode = GoalDialogMode.None;
            IsEditingLevelGoal = false;
        }

        [RelayCommand]
        private void CloseCelebration()
        {
            ShowCelebration = false;
        }

        [RelayCommand]
        public async Task SubmitDialog()
        {
            if(Character == null || _goalService == null)
            {
                return;
            }

            // Clear previous validation errors
            ValidationError = string.Empty;

            if(DialogMode == GoalDialogMode.Create)
            {
                string safeGoalTitle = UserInputSanitizer.TrimAndTruncate(GoalTitleInput, UserInputLimits.GoalTitleMaxLength);
                if(string.IsNullOrWhiteSpace(safeGoalTitle))
                {
                    ValidationError = _localizationService["Goal_ValidationTitleRequired"];
                    return;
                }

                if(UserInputSanitizer.ExceedsLength(GoalTitleInput, UserInputLimits.GoalTitleMaxLength))
                {
                    ValidationError = $"Goal title is too long (max {UserInputLimits.GoalTitleMaxLength} characters).";
                    return;
                }

                if(GoalTargetInput <= 0)
                {
                    ValidationError = _localizationService["Goal_ValidationTargetPositive"];
                    return;
                }

                GoalType type = NewGoalTypeIndex == 0 ? GoalType.Level : GoalType.Gold;
                long startValue = 0;

                if(type == GoalType.Level)
                {
                    startValue = GoalBaseLevelInput > 0 ? GoalBaseLevelInput : Character.Level;

                    if(GoalTargetInput <= startValue)
                    {
                        ValidationError = string.Format(_localizationService["Goal_ValidationTargetLevel"], startValue);
                        return;
                    }
                }

                CharacterGoalEntity newGoal = new()
                {
                    CharacterId = Character.Id,
                    Title = safeGoalTitle,
                    Type = type,
                    TargetValue = GoalTargetInput,
                    StartValue = startValue,
                    ManualProgressOffset = 0,
                    CreatedAt = GoalStartDate,
                    IsActive = true
                };
                await _goalService.AddGoalAsync(newGoal);
            }
            else if(DialogMode == GoalDialogMode.Edit)
            {
                string safeGoalTitle = UserInputSanitizer.TrimAndTruncate(GoalTitleInput, UserInputLimits.GoalTitleMaxLength);
                if(string.IsNullOrWhiteSpace(safeGoalTitle))
                {
                    ValidationError = _localizationService["Goal_ValidationTitleRequired"];
                    return;
                }

                if(UserInputSanitizer.ExceedsLength(GoalTitleInput, UserInputLimits.GoalTitleMaxLength))
                {
                    ValidationError = $"Goal title is too long (max {UserInputLimits.GoalTitleMaxLength} characters).";
                    return;
                }

                if(GoalTargetInput <= 0)
                {
                    ValidationError = _localizationService["Goal_ValidationTargetPositive"];
                    return;
                }

                if(_editingGoalEntity != null)
                {
                    _editingGoalEntity.Title = safeGoalTitle;
                    _editingGoalEntity.TargetValue = GoalTargetInput;
                    await _goalService.UpdateGoalAsync(_editingGoalEntity);
                }
            }
            else if(DialogMode == GoalDialogMode.AddProgress)
            {
                if(_editingGoalEntity != null && ProgressAddInput != 0)
                {
                    _editingGoalEntity.ManualProgressOffset += ProgressAddInput;
                    await _goalService.UpdateGoalAsync(_editingGoalEntity);
                }
            }

            await LoadGoals();
            CloseDialog();
        }

        [RelayCommand]
        public async Task DeleteGoal(GoalItemViewModel vm)
        {
            if(_goalService == null)
            {
                return;
            }
            await _goalService.DeleteGoalAsync(vm.Entity.Id);

            if(ActiveGoals.Contains(vm))
            {
                ActiveGoals.Remove(vm);
            }
            if(CompletedGoals.Contains(vm))
            {
                CompletedGoals.Remove(vm);
            }
            if(Goals.Contains(vm))
            {
                Goals.Remove(vm);
            }
        }

        // --- Standard Methods ---

        private async Task PrepareDeaths(Character character)
        {
            RecentDeaths.Clear();
            if(character.Deaths == null)
            {
                return;
            }

            IEnumerable<Death> sortedDeaths = character.Deaths.OrderByDescending(d => d.TimeUtc).Take(5);
            try
            {
                await _monsterImageCatalogService.EnsureCatalogAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monster image catalog was not ready while preparing death cards.");
            }

            foreach(Death death in sortedDeaths)
            {
                string killerName = _localizationService["Goal_KillerDeath"];
                string finalImageUri = _monsterImageCatalogService.DeathFallbackImageUri;

                if(!string.IsNullOrWhiteSpace(death.KillersJson))
                {
                    try
                    {
                        List<TibiaKiller>? killers = JsonSerializer.Deserialize<List<TibiaKiller>>(death.KillersJson);
                        TibiaKiller? mainKiller = killers?.LastOrDefault();

                        if(mainKiller != null)
                        {
                            killerName = mainKiller.Name;
                            if(!mainKiller.Player)
                            {
                                if(_monsterImageCatalogService.TryResolveImageUri(null, killerName, out string resolvedMonsterImageUri))
                                {
                                    finalImageUri = resolvedMonsterImageUri;
                                }
                            }
                            else
                            {
                                finalImageUri = _monsterImageCatalogService.PlayerKillerImageUri;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to parse death killer JSON for character '{CharacterName}'.", character.Name);
                    }
                }

                TimeSpan timeSpan = DateTimeOffset.UtcNow - death.TimeUtc;
                string timeAgo = timeSpan.TotalDays < 1 ? _localizationService["Goal_TimeToday"] : string.Format(_localizationService["Goal_TimeDaysAgo"], timeSpan.Days);

                RecentDeaths.Add(new DeathDisplayItem(timeAgo, $"Lvl {death.Level}", killerName, finalImageUri));
            }
        }
    }
}
