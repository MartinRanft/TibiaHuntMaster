using TibiaHuntMaster.Core.Content.Items;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Abstractions
{
    internal interface IItemsClient
    {
        Task<List<string>> GetItemNamesAsync(CancellationToken cancellationToken = default);
        Task<List<string>> GetItemCategoriesAsync(CancellationToken cancellationToken = default);
        Task<PagedResponseOfItemListItemResponse> GetPagedItemAsync(int page = 1,
            int pageSize = 100, CancellationToken cancellationToken = default);
        Task<ItemDetailsResponse> GetItemDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<List<ItemListItemResponse>> GetItemsByCategoryAsync(string category, CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);
    }
}