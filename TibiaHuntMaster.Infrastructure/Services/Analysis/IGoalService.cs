using TibiaHuntMaster.Infrastructure.Data.Entities.Character;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for managing character goals and tracking progress.
    /// </summary>
    public interface IGoalService
    {
        /// <summary>
        ///     Gets all goals for a character with current progress calculations.
        /// </summary>
        /// <param name="characterId">The character ID.</param>
        /// <param name="currentLevel">The character's current level.</param>
        /// <returns>List of goals with progress results.</returns>
        Task<List<GoalProgressResult>> GetGoalsForCharacterAsync(int characterId, int currentLevel);

        /// <summary>
        ///     Adds a new goal for a character.
        /// </summary>
        /// <param name="goal">The goal to add.</param>
        Task AddGoalAsync(CharacterGoalEntity goal);

        /// <summary>
        ///     Updates an existing goal.
        /// </summary>
        /// <param name="goal">The goal to update.</param>
        Task UpdateGoalAsync(CharacterGoalEntity goal);

        /// <summary>
        ///     Deletes a goal by ID.
        /// </summary>
        /// <param name="goalId">The goal ID to delete.</param>
        Task DeleteGoalAsync(int goalId);

        /// <summary>
        ///     Processes hunt session and updates goal progress.
        /// </summary>
        /// <param name="charId">Character ID.</param>
        /// <param name="soloSessionId">Solo hunt session ID (optional).</param>
        /// <param name="teamSessionId">Team hunt session ID (optional).</param>
        /// <param name="balanceChange">Balance change from hunt.</param>
        /// <param name="xpChange">XP change from hunt.</param>
        Task ProcessHuntProgressAsync(int charId, int? soloSessionId, int? teamSessionId, long balanceChange, long xpChange);

        /// <summary>
        ///     Gets all goals for a character (including inactive/completed).
        /// </summary>
        /// <param name="charId">Character ID.</param>
        /// <returns>List of all goals.</returns>
        Task<List<CharacterGoalEntity>> GetAllGoalsAsync(int charId);
    }
}
