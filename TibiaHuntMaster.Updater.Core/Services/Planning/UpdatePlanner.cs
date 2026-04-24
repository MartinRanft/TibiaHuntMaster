using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

using TibiaHuntMaster.Updater.Core.Abstractions;
using TibiaHuntMaster.Updater.Core.Constants;
using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.Updater.Core.Services.Planning
{
    public sealed class UpdatePlanner : IUpdatePlanner
    {
        private Uri StableChannelUri { get; } = UpdateFeedConstants.StableFeedUri;
        private HttpClient Client { get; } = new();
        
        public async Task<UpdateCheckResult?> CheckForUpdateAsync(string currentVersion
            , CancellationToken cancellationToken = default)
        {
            try
            {
                ReleaseFeedResponse? onlineVersion = await GetUpdateCheckResultAsync(cancellationToken);

                if(onlineVersion is null)
                {
                    return new UpdateCheckResult()
                    {
                        CurrentVersion = currentVersion,
                        ErrorMessage = "The update feed returned no content.",
                        Status = UpdateCheckStatus.InvalidFeed,
                        LatestVersion = null,
                        UpdatePlan = null
                    };
                }

                bool updateAvailable = VersionHelper.IsRemoteVersionNewer(currentVersion, onlineVersion.Version);

                if(!updateAvailable)
                {
                    return new UpdateCheckResult()
                    {
                        CurrentVersion = currentVersion,
                        ErrorMessage = "",
                        Status = UpdateCheckStatus.UpToDate,
                        LatestVersion = onlineVersion.Version,
                        UpdatePlan = null
                    };
                }

                ReleaseFeedAssetResponse? asset = GetAssetForCurrentPlatform(onlineVersion);
                
                if(asset is null)
                {
                    return new UpdateCheckResult()
                    {
                        CurrentVersion = currentVersion,
                        ErrorMessage = "No asset available for the current platform.",
                        Status = UpdateCheckStatus.UnsupportedPlatform,
                        LatestVersion = onlineVersion.Version,
                        UpdatePlan = null
                    };
                }

                return new UpdateCheckResult()
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = "",
                    Status = UpdateCheckStatus.UpdateAvailable,
                    LatestVersion = onlineVersion.Version,
                    UpdatePlan = new UpdatePlan
                    {
                        CurrentVersion = currentVersion,
                        TargetVersion = onlineVersion.Version,
                        Tag = onlineVersion.Tag,
                        Channel = onlineVersion.Channel,
                        PublishedAtUtc = onlineVersion.PublishedAtUtc,
                        Asset = asset
                    }
                };
            }
            catch(HttpRequestException ex)
            {
                return new UpdateCheckResult()
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = ex.Message,
                    Status = UpdateCheckStatus.FeedUnavailable,
                    LatestVersion = null,
                    UpdatePlan = null
                };
            }
            catch(JsonException ex)
            {
                return new UpdateCheckResult()
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = ex.Message,
                    Status = UpdateCheckStatus.InvalidFeed,
                    LatestVersion = null,
                    UpdatePlan = null
                };
            }
            catch(NotSupportedException ex)
            {
                return new UpdateCheckResult()
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = ex.Message,
                    Status = UpdateCheckStatus.InvalidFeed,
                    LatestVersion = null,
                    UpdatePlan = null
                };
            }
            catch(Exception ex)
            {
                return new UpdateCheckResult()
                {
                    CurrentVersion = currentVersion,
                    ErrorMessage = ex.Message,
                    Status = UpdateCheckStatus.Failed,
                    LatestVersion = null,
                    UpdatePlan = null
                };
            }
        }
        
        public async Task<UpdateDownloadResult> DownloadUpdateAsync(
            UpdatePlan updatePlan,
            string appBaseDirectory,
            IProgress<UpdateDownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(appBaseDirectory))
            {
                throw new ArgumentException("App base directory cannot be null or empty.", nameof(appBaseDirectory));
            }

            if(updatePlan.Asset is null)
            {
                return new UpdateDownloadResult()
                {
                    Status = UpdateDownloadStatus.Failed,
                    ErrorMessage = "No asset available for the current platform.",
                    DownloadFilePath = null,
                    UpdatePlan = updatePlan
                };
            }
            
            if(string.IsNullOrWhiteSpace(updatePlan.Asset.Url))
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = null,
                    ErrorMessage = "The update asset URL is missing."
                };
            }
            
            string fileName = Path.GetFileName(updatePlan.Asset.FileName);
            if(string.IsNullOrWhiteSpace(fileName))
            {
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = null,
                    ErrorMessage = "The update asset file name is invalid."
                };
            }
            
            string cacheDownloadsDirectory = Path.Combine(appBaseDirectory, "Cache", "Downloads");
            Directory.CreateDirectory(cacheDownloadsDirectory);

            string downloadFilePath = Path.Combine(cacheDownloadsDirectory, fileName);
            string tempFilePath = $"{downloadFilePath}.download";
            
            if(File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            try
            {
                using HttpResponseMessage response = await Client.GetAsync(updatePlan.Asset.Url,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
                response.EnsureSuccessStatusCode();
            
                long? totalBytes = response.Content.Headers.ContentLength;
                long bytesReceived = 0;
            
                progress?.Report(new UpdateDownloadProgress()
                {
                    BytesReceived = bytesReceived,
                    TotalBytes = totalBytes
                });
            
                byte[] buffer = new byte[81920];
            
                await using Stream sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using FileStream targetStream = new(
                    tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, useAsync:true);
            
                while(true)
                {
                    int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

                    if(bytesRead == 0)
                    {
                        break;
                    }

                    await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    bytesReceived += bytesRead;
                
                    progress?.Report(new UpdateDownloadProgress()
                    {
                        BytesReceived = bytesReceived,
                        TotalBytes = totalBytes
                    });
                }
            
                await targetStream.FlushAsync(cancellationToken);

                if(File.Exists(downloadFilePath))
                {
                    File.Delete(downloadFilePath);
                }
                
                File.Move(tempFilePath, downloadFilePath);
                
                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Succeeded,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = downloadFilePath,
                    ErrorMessage = null
                };
            }
            catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
            {
                if(File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Cancelled,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = null,
                    ErrorMessage = "The update download was cancelled."
                };
            }
            catch(Exception ex)
            {
                if(File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                return new UpdateDownloadResult
                {
                    Status = UpdateDownloadStatus.Failed,
                    UpdatePlan = updatePlan,
                    DownloadFilePath = null,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<ReleaseFeedResponse?> GetUpdateCheckResultAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await Client.GetAsync(StableChannelUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            ReleaseFeedResponse? result = await response.Content.ReadFromJsonAsync<ReleaseFeedResponse>(cancellationToken);
            
            return result;
        }
        
        private ReleaseFeedAssetResponse? GetAssetForCurrentPlatform(ReleaseFeedResponse release)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return release.WindowsX64;
            }
            
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return release.LinuxX64;
            }

            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? release.OsxX64
                : release.OsxArm64;
            }
            return null;
        }
    }
}
