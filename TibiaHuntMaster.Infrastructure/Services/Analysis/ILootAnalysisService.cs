using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for analyzing hunt loot and grouping by vendor.
    /// </summary>
    public interface ILootAnalysisService
    {
        /// <summary>
        ///     Analyzes loot from a specific hunt session.
        /// </summary>
        /// <param name="sessionId">Hunt session ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Loot grouped by vendor.</returns>
        Task<List<LootGroup>> AnalyzeSessionLootAsync(int sessionId, CancellationToken ct = default);

        /// <summary>
        ///     Analyzes a list of loot entries and groups by vendor.
        /// </summary>
        /// <param name="lootEntries">List of loot entries.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Loot grouped by vendor.</returns>
        Task<List<LootGroup>> AnalyzeLootListAsync(List<HuntLootEntry> lootEntries, CancellationToken ct = default);
    }
}
