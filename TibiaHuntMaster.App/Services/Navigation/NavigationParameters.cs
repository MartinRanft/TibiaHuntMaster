using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.App.Services.Navigation
{
    /// <summary>
    ///     Type-safe navigation parameters for common navigation scenarios.
    /// </summary>
    internal static class NavigationParameters
    {
        /// <summary>
        ///     Parameter for navigating to History with a specific goal filter.
        /// </summary>
        internal sealed record HistoryWithGoalFilter(string CharacterName, CharacterGoalEntity Goal);

        /// <summary>
        ///     Parameter for navigating to Hunt Analyzer with an existing session.
        /// </summary>
        internal sealed record AnalyzerWithSession(string CharacterName, HuntSessionEntity Session, List<int> SourceIds);

        /// <summary>
        ///     Parameter for navigating to Hunt Analyzer with a team session.
        /// </summary>
        internal sealed record AnalyzerWithTeamSession(string CharacterName, TeamHuntSessionEntity TeamSession);

        /// <summary>
        ///     Parameter for navigating to Imbuement Configuration.
        /// </summary>
        internal sealed record ImbuementConfiguration(int CharacterId, Action? OnClose = null);

        /// <summary>
        ///     Parameter for navigating to Dashboard with a character.
        /// </summary>
        internal sealed record DashboardWithCharacter(Character Character);

        /// <summary>
        ///     Parameter for navigating to Overview.
        /// </summary>
        internal sealed record OverviewWithCharacter(Character Character);

        /// <summary>
        ///     Parameter for navigating to Progress analytics.
        /// </summary>
        internal sealed record ProgressWithCharacter(Character Character);

        /// <summary>
        ///     Parameter for navigating to Economy analytics.
        /// </summary>
        internal sealed record EconomyWithCharacter(Character Character);

        /// <summary>
        ///     Parameter for navigating to Hunt Analyzer.
        /// </summary>
        internal sealed record AnalyzerWithCharacter(string CharacterName);

        /// <summary>
        ///     Parameter for navigating to History.
        /// </summary>
        internal sealed record HistoryWithCharacter(string CharacterName);

        /// <summary>
        ///     Parameter for navigating to Hunting Places.
        /// </summary>
        internal sealed record HuntingPlacesWithVocation(string Vocation);

        /// <summary>
        ///     Parameter for navigating to Hunting Places with active character context.
        /// </summary>
        internal sealed record HuntingPlacesWithCharacter(Character Character);

        /// <summary>
        ///     Parameter for navigating to Goal History.
        /// </summary>
        internal sealed record GoalHistoryWithCharacter(string CharacterName, int CharacterId, int CurrentLevel);
        
        /// <summary>
        ///     Parameter for navigating to Tibia Map.
        /// </summary>
        internal sealed record MinimapWithCharacter(Character Character);
        
        public sealed record MinimapWithTarget(Character Character, int X, int Y, byte Z);
    }
}
