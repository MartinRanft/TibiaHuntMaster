using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IUpdatePlanner
    {
        Task<UpdateCheckResult?> CheckForUpdateAsync(
            string currentVersion,
            CancellationToken cancellationToken = default);

        Task<UpdateDownloadResult> DownloadUpdateAsync(
            UpdatePlan updatePlan,
            string appBaseDirectory,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
