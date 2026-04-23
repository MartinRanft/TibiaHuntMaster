using TibiaHuntMaster.Core.Content.HuntingPlaces;
using TibiaHuntMaster.Core.Content.Sync;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Abstractions
{
    public interface IHuntingPlacesClient
    {
        Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default);
        Task<HuntingPlaceDetailsResponse> GetHuntingPlaceDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);
    }
}