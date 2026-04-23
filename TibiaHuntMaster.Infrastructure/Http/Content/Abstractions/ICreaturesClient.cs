using TibiaHuntMaster.Core.Content.Creatures;
using TibiaHuntMaster.Core.Content.Loot;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Abstractions
{
    public interface ICreaturesClient
    {
        Task<PagedResponseOfCreatureListItemResponse> GetPagedCreatureAsync(
            int page = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);
        Task<List<string>> GetCreatureNamesAsync(CancellationToken cancellationToken = default);
        Task<CreatureDetailsResponse> GetCreatureDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<LootStatisticDetailsResponse> GetCreatureLootStatisticsAsync(int id, CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);
    }
}