using System.Globalization;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.HuntingPlaces;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Http.Content.Shared;

namespace TibiaHuntMaster.Infrastructure.Http.Content.HuntingPlaces
{
    public class HuntingPlacesClient(HttpClient httpClient,ILogger<HuntingPlacesClient> logger)
    : ContentHttpClientBase<HuntingPlacesClient>(httpClient, logger), IHuntingPlacesClient
    {
        public Task<List<HuntingPlaceListItemResponse>> GetHuntingPlacesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<HuntingPlaceListItemResponse>>("/api/v1/hunting-places/list", cancellationToken);
        
        public async Task<HuntingPlaceDetailsResponse> GetHuntingPlaceDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            if(id >= 1)
            {
                return await GetJsonAsync<HuntingPlaceDetailsResponse>($"/api/v1/hunting-places/{id}", cancellationToken);
            }
            Logger.LogWarning("Invalid hunting place id {HuntingPlaceId}. Hunting place id must be greater than 0.", id);
            throw new ArgumentOutOfRangeException(nameof(id), id, "Hunting place id must be greater than 0.");
        }
        
        public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<SyncStateResponse>>("/api/v1/hunting-places/sync", cancellationToken);
        
        public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default) =>
             GetJsonAsync<List<SyncStateResponse>>(
                $"/api/v1/hunting-places/sync/by-date?time={Uri.EscapeDataString(date.ToString("O", CultureInfo.InvariantCulture))}",
                cancellationToken);
    }
}
