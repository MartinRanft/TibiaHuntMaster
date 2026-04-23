using TibiaHuntMaster.Core.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    /// <summary>
    ///     Service for managing hunt sessions, statistics, and supply adjustments.
    /// </summary>
    public interface IHuntSessionService
    {
        /// <summary>
        ///     Imports a hunt session into the database based on the provided options.
        /// </summary>
        /// <param name="options">The options containing hunt session details.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result, session entity, and error message if any.</returns>
        Task<(SessionImportResult Result, HuntSessionEntity? Session, string? Error)> ImportSessionAsync(
            SessionImportOptions options,
            CancellationToken ct = default);

        /// <summary>
        ///     Retrieves hunt statistics for a specified character.
        /// </summary>
        /// <param name="charName">Character name.</param>
        /// <param name="limit">Maximum number of sessions to include.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Hunt statistics or null if no sessions found.</returns>
        Task<HuntStatistics?> GetStatisticsAsync(string charName, int limit = 10, CancellationToken ct = default);

        /// <summary>
        ///     Adds a new supply adjustment to the database.
        /// </summary>
        /// <param name="adj">The adjustment to add.</param>
        Task AddAdjustmentAsync(HuntSupplyAdjustment adj);

        /// <summary>
        ///     Removes a supply adjustment from the database.
        /// </summary>
        /// <param name="adj">The adjustment to remove.</param>
        Task RemoveAdjustmentAsync(HuntSupplyAdjustment adj);

        /// <summary>
        ///     Updates an existing supply adjustment.
        /// </summary>
        /// <param name="adj">The adjustment to update.</param>
        Task UpdateAdjustmentAsync(HuntSupplyAdjustment adj);

        /// <summary>
        ///     Replaces all supply adjustments for a session with new ones.
        /// </summary>
        /// <param name="session">Session ID.</param>
        /// <param name="newAdjustments">New adjustments to replace with.</param>
        Task ReplacedAdjustmentAsync(int session, List<HuntSupplyAdjustment> newAdjustments);

        /// <summary>
        ///     Updates an existing hunt session.
        /// </summary>
        /// <param name="currentSession">The session to update.</param>
        Task UpdateSessionAsync(HuntSessionEntity currentSession);

        /// <summary>
        ///     Backfills missing hunting-place links for existing solo hunt sessions.
        /// </summary>
        /// <param name="batchSize">Maximum sessions per batch.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Number of sessions updated with a hunting place link.</returns>
        Task<int> BackfillMissingHuntingPlaceLinksAsync(int batchSize = 300, CancellationToken ct = default);
    }
}
