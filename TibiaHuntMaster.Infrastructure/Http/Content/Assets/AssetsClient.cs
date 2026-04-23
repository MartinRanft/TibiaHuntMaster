using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.Assets;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Http.Content.Shared;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Assets
{
    public sealed class AssetsClient(HttpClient httpClient, ILogger<AssetsClient> logger)
        : ContentHttpClientBase<AssetsClient>(httpClient, logger), IAssetsClient
    {
        public async Task<AssetMetadataResponse> GetAssetMetadataAsync(int id, CancellationToken cancellationToken = default)
        {
            if(id < 1)
            {
                Logger.LogWarning("Invalid asset id {AssetId}. Asset id must be greater than 0.", id);
                throw new ArgumentOutOfRangeException(nameof(id), id, "Asset id must be greater than 0.");
            }

            return await GetJsonAsync<AssetMetadataResponse>($"/api/v1/assets/metadata/{id}", cancellationToken);
        }

        public async Task<List<AssetMetadataResponse>> SearchAssetMetadataAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(fileName))
            {
                Logger.LogWarning("Invalid fileName value {FileName}. File name must not be null or whitespace.", fileName);
                throw new ArgumentException("File name must not be null or whitespace.", nameof(fileName));
            }

            return await GetJsonAsync<List<AssetMetadataResponse>>(
                $"/api/v1/assets/metadata/search?fileName={Uri.EscapeDataString(fileName)}",
                cancellationToken);
        }

        public async Task DownloadAssetAsync(int id, Stream destination, CancellationToken cancellationToken = default)
        {
            if(id < 1)
            {
                Logger.LogWarning("Invalid asset id {AssetId}. Asset id must be greater than 0.", id);
                throw new ArgumentOutOfRangeException(nameof(id), id, "Asset id must be greater than 0.");
            }

            await DownloadToStreamAsync($"/api/v1/assets/{id}", destination, cancellationToken);
        }
    }
}
