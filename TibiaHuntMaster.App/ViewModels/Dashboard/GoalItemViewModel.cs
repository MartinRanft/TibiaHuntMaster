using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class GoalItemViewModel : ObservableObject
    {
        [ObservableProperty]private double _percentage;

        public GoalItemViewModel(GoalProgressResult result)
        {
            Entity = result.Goal;
            Percentage = Math.Min(100, Math.Max(0, result.Percentage));

            // Prozent Text (z.B. "45%")
            PercentText = $"{Percentage:F1}%";

            // Werte Formatierung (z.B. "16,5kk / 50kk" oder "591 / 600")
            string current = FormatValue(result.CurrentValue, Entity.Type);
            string target = FormatValue(Entity.TargetValue, Entity.Type);

            ProgressText = $"{current} / {target}";
        }

        public CharacterGoalEntity Entity { get; }

        // Texte für die UI
        public string ProgressText { get; }

        public string PercentText { get; }

        public string Icon => Entity.Type == GoalType.Gold ? "💰" : "📈";

        public IBrush ProgressColor => Entity.Type == GoalType.Gold
        ? SolidColorBrush.Parse("#FFC107") // Gold
        : SolidColorBrush.Parse("#2196F3"); // Blau

        private string FormatValue(long value, GoalType type)
        {
            if(type == GoalType.Level)
            {
                return value.ToString("N0");
            }

            // Tibia Currency Logic für Gold
            double abs = Math.Abs((double)value);
            string prefix = value < 0 ? "-" : "";

            if(abs >= 1_000_000_000)
            {
                return $"{prefix}{abs / 1_000_000_000.0:0.##}kkk";
            }
            if(abs >= 1_000_000)
            {
                return $"{prefix}{abs / 1_000_000.0:0.##}kk";
            }
            if(abs >= 1_000)
            {
                return $"{prefix}{abs / 1_000.0:0.#}k";
            }

            return value.ToString("N0");
        }
    }
}