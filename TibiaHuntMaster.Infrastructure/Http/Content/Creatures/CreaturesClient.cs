using System.Globalization;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.Creatures;
using TibiaHuntMaster.Core.Content.Loot;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Http.Content.Shared;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Creatures
{
    public sealed class CreaturesClient(HttpClient httpClient, ILogger<CreaturesClient> logger) 
    : ContentHttpClientBase<CreaturesClient>(httpClient, logger), ICreaturesClient
    {
        public Task<PagedResponseOfCreatureListItemResponse> GetPagedCreatureAsync(int page = 1, int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            if(page < 1)
            {
                Logger.LogWarning("Invalid page value {Page}. Page must be greater than 0.", page);
                throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be greater than 0.");
            }

            switch (pageSize)
            {
                case < 1:
                    Logger.LogWarning("Invalid pageSize value {PageSize}. Page size must be greater than 0.", pageSize);
                    throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be greater than 0.");
                case > 100:
                    Logger.LogWarning("Invalid pageSize value {PageSize}. Page size must be less than or equal to 100.", pageSize);
                    throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be less than or equal to 100.");
                default:
                    return GetJsonAsync<PagedResponseOfCreatureListItemResponse>(
                        $"/api/v1/creatures?page={page}&pageSize={pageSize}",
                        cancellationToken);
            }
        }
        
        public Task<List<string>> GetCreatureNamesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<string>>("/api/v1/creatures/list", cancellationToken);

        public async Task<CreatureDetailsResponse> GetCreatureDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            if(id >= 1)
            {
                return await GetJsonAsync<CreatureDetailsResponse>($"/api/v1/creatures/{id}", cancellationToken);
            }
            Logger.LogWarning("Invalid creature id {CreatureId}. Creature id must be greater than 0.", id);
            throw new ArgumentOutOfRangeException(nameof(id), id, "Creature id must be greater than 0.");
        }

        public async Task<LootStatisticDetailsResponse> GetCreatureLootStatisticsAsync(int id, CancellationToken cancellationToken = default)
        {
            if(id >= 1)
            {
                return await GetJsonAsync<LootStatisticDetailsResponse>($"/api/v1/creatures/{id}/loot", cancellationToken);
            }
            Logger.LogWarning("Invalid creature id {CreatureId}. Creature id must be greater than 0.", id);
            throw new ArgumentOutOfRangeException(nameof(id), id, "Creature id must be greater than 0.");
        }
        
        public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<SyncStateResponse>>("/api/v1/creatures/sync", cancellationToken);
        
        public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<SyncStateResponse>>(
                $"/api/v1/creatures/sync/by-date?time={Uri.EscapeDataString(date.ToString("O", CultureInfo.InvariantCulture))}",
                cancellationToken);
    }
}
