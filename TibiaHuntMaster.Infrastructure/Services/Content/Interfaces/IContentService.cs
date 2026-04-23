using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Interfaces
{
    public interface IContentService
    {
        /// <summary>
        /// Ensures that the content is properly initialized and ready for use.
        /// </summary>
        /// <param name="ct">
        /// The cancellation token to observe while waiting for the operation to complete.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="ContentInitializationResult"/> that indicates the outcome of the initialization process.
        /// </returns>
        Task<ContentInitializationResult> EnsureContentInitializedAsync(CancellationToken ct = default);

        /// <summary>
        /// Refreshes the content to ensure that it is up-to-date and synchronized with the source.
        /// </summary>
        /// <param name="ct">
        /// The cancellation token to observe while waiting for the operation to complete.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="ContentRefreshResult"/> that describes the outcome of the refresh process.
        /// </returns>
        Task<ContentRefreshResult> RefreshContentAsync(CancellationToken ct = default);

        /// <summary>
        /// Re-initializes the content by clearing all existing data and re-downloading it from the source.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains a <see cref="ContentReInitializationResult"/> that describes the outcome of the full content rebuild.
        /// </returns>
        Task<ContentReInitializationResult> ReInitializeContentAsync(CancellationToken ct = default);
    }
}
