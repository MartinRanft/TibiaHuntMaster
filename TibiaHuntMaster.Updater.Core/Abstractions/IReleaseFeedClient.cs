using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IReleaseFeedClient
    {
        Task<ReleaseFeedResponse> GetLatestReleaseAsync(
            Uri feedUri,
            CancellationToken cancellationToken = default);
    }
}
