using System.Runtime.InteropServices;
using TibiaHuntMaster.Updater.Core.Abstractions;
using TibiaHuntMaster.Updater.Core.Constants;
using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.Updater.Core.Services.Planning
{
    public sealed class UpdatePlanner(
        IReleaseFeedClient feedClient,
        IUpdatePackageDownloader packageDownloader,
        IVersionComparer versionComparer) : IUpdatePlanner
    {
        private static readonly Uri StableChannelUri = UpdateFeedConstants.StableFeedUri;

        public async Task<UpdateCheckResult?> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default)
        {
            try
            {
                ReleaseFeedResponse onlineVersion = await feedClient.GetLatestReleaseAsync(StableChannelUri, cancellationToken);

                bool updateAvailable = versionComparer.Compare(currentVersion, onlineVersion.Version) > 0;

                if (!updateAvailable)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = currentVersion,
                        Status = UpdateCheckStatus.UpToDate,
                        LatestVersion = onlineVersion.Version,
                        UpdatePlan = null,
                        ErrorMessage = string.Empty
                    };
                }

                ReleaseFeedAssetResponse? asset = GetAssetForCurrentPlatform(onlineVersion);

                if (asset is null)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = currentVersion,
                        Status = UpdateCheckStatus.UnsupportedPlatform,
                        LatestVersion = onlineVersion.Version,
                        UpdatePlan = null,
                        ErrorMessage = "No asset available for the current platform."
                    };
                }

                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Status = UpdateCheckStatus.UpdateAvailable,
                    LatestVersion = onlineVersion.Version,
                    ErrorMessage = string.Empty,
                    UpdatePlan = new UpdatePlan
                    {
                        CurrentVersion = currentVersion,
                        TargetVersion = onlineVersion.Version,
                        Tag = onlineVersion.Tag,
                        Channel = onlineVersion.Channel,
                        PublishedAtUtc = onlineVersion.PublishedAtUtc,
                        Asset = asset,
                        ReleasePageUrl = onlineVersion.ReleasePageUrl
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Status = UpdateCheckStatus.FeedUnavailable,
                    LatestVersion = null,
                    UpdatePlan = null,
                    ErrorMessage = ex.Message
                };
            }
            catch (InvalidOperationException ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Status = UpdateCheckStatus.InvalidFeed,
                    LatestVersion = null,
                    UpdatePlan = null,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Status = UpdateCheckStatus.Failed,
                    LatestVersion = null,
                    UpdatePlan = null,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<UpdateDownloadResult> DownloadUpdateAsync(
            UpdatePlan updatePlan,
            string appBaseDirectory,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(appBaseDirectory))
                throw new ArgumentException("App base directory cannot be null or empty.", nameof(appBaseDirectory));

            if (updatePlan.Asset is null)
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    ErrorMessage = "No asset available for the current platform."
                };
            }

            if (string.IsNullOrWhiteSpace(updatePlan.Asset.Url))
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    ErrorMessage = "The update asset URL is missing."
                };
            }

            string fileName = Path.GetFileName(updatePlan.Asset.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    ErrorMessage = "The update asset file name is invalid."
                };
            }

            string downloadsDirectory = Path.Combine(appBaseDirectory, "Cache", "Downloads");
            Directory.CreateDirectory(downloadsDirectory);

            string downloadFilePath = Path.Combine(downloadsDirectory, fileName);

            try
            {
                string resultPath = await packageDownloader.DownloadPackageAsync(
                    new Uri(updatePlan.Asset.Url),
                    downloadFilePath,
                    updatePlan.Asset.Sha256,
                    cancellationToken);

                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Succeeded,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = resultPath,
                    ErrorMessage = null
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Cancelled,
                    UpdatePlan = updatePlan,
                    ErrorMessage = "The update download was cancelled."
                };
            }
            catch (Exception ex)
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static ReleaseFeedAssetResponse? GetAssetForCurrentPlatform(ReleaseFeedResponse release)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return release.WindowsX64;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return release.LinuxX64;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.X64
                    ? release.OsxX64
                    : release.OsxArm64;
            }

            return null;
        }
    }
}
