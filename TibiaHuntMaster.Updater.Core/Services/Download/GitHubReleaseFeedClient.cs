using System.Net.Http.Json;
using TibiaHuntMaster.Updater.Core.Abstractions;
using TibiaHuntMaster.Updater.Core.Models;

namespace TibiaHuntMaster.Updater.Core.Services.Download
{
    public sealed class GitHubReleaseFeedClient(HttpClient httpClient) : IReleaseFeedClient
    {
        public async Task<ReleaseFeedResponse> GetLatestReleaseAsync(Uri feedUri, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(feedUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            ReleaseFeedResponse? result = await response.Content.ReadFromJsonAsync<ReleaseFeedResponse>(cancellationToken);

            return result ?? throw new InvalidOperationException("The release feed returned no content.");
        }
    }
}