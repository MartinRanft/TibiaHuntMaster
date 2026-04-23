using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

using static TibiaHuntMaster.App.Services.Navigation.NavigationParameters;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class GoalHistoryViewModel : ViewModelBase, INavigationAware
    {
        private readonly IGoalService _goalService;
        private readonly ILocalizationService _localizationService;
        [ObservableProperty]private ObservableCollection<GoalHistoryItem> _abandonedGoals = [];

        [ObservableProperty]private string _characterName = string.Empty;
        [ObservableProperty]private ObservableCollection<GoalHistoryItem> _completedGoals = [];
        [ObservableProperty]private bool _isLoading;

        public GoalHistoryViewModel(IGoalService goalService, ILocalizationService localizationService)
        {
            _goalService = goalService;
            _localizationService = localizationService;
        }

        public void OnNavigatedTo(object? parameter)
        {
            if(parameter is GoalHistoryWithCharacter historyParam)
            {
                CharacterName = historyParam.CharacterName;
                _ = LoadGoalHistoryAsync(historyParam.CharacterId, historyParam.CurrentLevel);
            }
        }

        public void OnNavigatedFrom()
        {
            // Cleanup if needed
        }

        private async Task LoadGoalHistoryAsync(int characterId, int currentLevel)
        {
            IsLoading = true;

            List<CharacterGoalEntity> allGoals = await _goalService.GetAllGoalsAsync(characterId);
            List<GoalProgressResult> progressResults = await _goalService.GetGoalsForCharacterAsync(characterId, currentLevel);

            CompletedGoals.Clear();
            AbandonedGoals.Clear();

            foreach(CharacterGoalEntity goal in allGoals)
            {
                if(!goal.IsActive)
                {
                    // Abandoned goals (manually deactivated)
                    GoalProgressResult? progress = progressResults.Find(p => p.Goal.Id == goal.Id);
                    AbandonedGoals.Add(new GoalHistoryItem(goal, progress?.CurrentValue ?? 0, progress?.Percentage ?? 0, _localizationService));
                }
                else if(goal.IsCompleted)
                {
                    // Completed goals
                    GoalProgressResult? progress = progressResults.Find(p => p.Goal.Id == goal.Id);
                    CompletedGoals.Add(new GoalHistoryItem(goal, progress?.CurrentValue ?? 0, progress?.Percentage ?? 0, _localizationService));
                }
            }

            IsLoading = false;
        }
    }

    public sealed class GoalHistoryItem : ObservableObject
    {
        private readonly ILocalizationService _localizationService;

        public GoalHistoryItem(CharacterGoalEntity entity, long currentValue, double percentage, ILocalizationService localizationService)
        {
            Entity = entity;
            CurrentValue = currentValue;
            Percentage = percentage;
            _localizationService = localizationService;
        }

        public CharacterGoalEntity Entity { get; }

        public long CurrentValue { get; }

        public double Percentage { get; }

        public string Title => Entity.Title;

        public string TypeIcon => Entity.Type switch
        {
            GoalType.Level => "⬆️",
            GoalType.Gold => "💰",
            GoalType.Bestiary => "📖",
            _ => "❓"
        };

        public string ProgressDisplay => Entity.Type == GoalType.Level
        ? string.Format(_localizationService["GoalHistory_ProgressLevel"], CurrentValue, Entity.TargetValue)
        : string.Format(_localizationService["GoalHistory_ProgressGold"], CurrentValue.ToString("N0"), Entity.TargetValue.ToString("N0"));

        public string CompletedDate => Entity.IsCompleted
        ? string.Format(_localizationService["GoalHistory_CompletedDate"], Entity.CreatedAt.ToString("dd.MM.yyyy"))
        : string.Format(_localizationService["GoalHistory_AbandonedDate"], Entity.CreatedAt.ToString("dd.MM.yyyy"));
    }
}