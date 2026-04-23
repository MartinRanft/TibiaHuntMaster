namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for searching creatures and items in the game glossary.
    /// </summary>
    public interface IGlossarySearchService
    {
        /// <summary>
        ///     Finds creatures that drop a specific item.
        /// </summary>
        /// <param name="itemNameFragment">Item name or fragment.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of creatures dropping the item.</returns>
        Task<List<CreatureEntitys>> FindCreaturesDroppingAsync(string itemNameFragment, CancellationToken ct = default);

        /// <summary>
        ///     Searches for items by name query.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of matching items.</returns>
        Task<List<ItemEntity>> SearchItemsAsync(string query, CancellationToken ct = default);
    }
}
