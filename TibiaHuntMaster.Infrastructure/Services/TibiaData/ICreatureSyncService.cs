namespace TibiaHuntMaster.Infrastructure.Services.TibiaData
{
    /// <summary>
    ///     Service for syncing creature data from TibiaData API.
    /// </summary>
    public interface ICreatureSyncService
    {
        /// <summary>
        ///     Syncs creatures from TibiaData API to the database.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        Task SyncCreaturesAsync(CancellationToken ct = default);

        /// <summary>
        ///     Gets the list of boosted creatures.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of boosted creatures.</returns>
        Task<List<CreatureEntitys>> GetBoostedCreaturesAsync(CancellationToken ct = default);

        /// <summary>
        ///     Gets the list of creatures with images.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of creatures with images.</returns>
        Task<List<CreatureEntitys>> GetCreaturesWithImage(CancellationToken ct = default);
    }
}
