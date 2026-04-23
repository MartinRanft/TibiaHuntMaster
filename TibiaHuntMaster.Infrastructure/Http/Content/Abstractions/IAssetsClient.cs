using TibiaHuntMaster.Core.Content.Assets;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Abstractions
{
    internal interface IAssetsClient
    {
        Task<AssetMetadataResponse> GetAssetMetadataAsync(int id, CancellationToken cancellationToken = default);
        Task<List<AssetMetadataResponse>> SearchAssetMetadataAsync(string fileName, CancellationToken cancellationToken = default);
        Task DownloadAssetAsync(int id, Stream destination, CancellationToken cancellationToken = default);
    }
}
