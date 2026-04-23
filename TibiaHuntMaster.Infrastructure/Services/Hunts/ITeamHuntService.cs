using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    /// <summary>
    ///     Service for managing team hunt sessions.
    /// </summary>
    public interface ITeamHuntService
    {
        /// <summary>
        ///     Imports a team hunt session into the database.
        /// </summary>
        /// <param name="rawText">Raw text from analyzer.</param>
        /// <param name="characterName">Character name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result, session entity, and error message if any.</returns>
        Task<(SessionImportResult Result, TeamHuntSessionEntity? Session, string? Error)> ImportTeamSessionAsync(
            string rawText,
            string characterName,
            CancellationToken ct = default);

        /// <summary>
        ///     Enriches the latest team hunt session with XP data if it matches the provided time and character.
        /// </summary>
        /// <param name="charName">Character name.</param>
        /// <param name="startTime">Session start time.</param>
        /// <param name="xpGain">XP gained.</param>
        /// <param name="xpPerHour">XP per hour.</param>
        /// <returns>The enriched session or null if no matching session found.</returns>
        Task<TeamHuntSessionEntity?> EnrichLatestSessionIfMatching(
            string charName,
            DateTimeOffset startTime,
            long xpGain,
            long xpPerHour);

        /// <summary>
        ///     Updates an existing team hunt session.
        /// </summary>
        /// <param name="currentTeamSession">The session to update.</param>
        Task UpdateSessionAsync(TeamHuntSessionEntity currentTeamSession);
    }
}
