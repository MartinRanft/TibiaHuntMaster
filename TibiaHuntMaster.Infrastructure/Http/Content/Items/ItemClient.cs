using System.Globalization;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.Items;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Http.Content.Shared;

namespace TibiaHuntMaster.Infrastructure.Http.Content.Items
{
    public sealed class ItemClient(HttpClient httpClient,ILogger<ItemClient> logger)
    : ContentHttpClientBase<ItemClient>(httpClient, logger), IItemsClient
    {
        public Task<List<string>> GetItemNamesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<string>>("/api/v1/items/list", cancellationToken);
        
        public Task<List<string>> GetItemCategoriesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<string>>("/api/v1/items/categories", cancellationToken);
        
        public async Task<PagedResponseOfItemListItemResponse> GetPagedItemAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
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
                    return await GetJsonAsync<PagedResponseOfItemListItemResponse>($"/api/v1/items?page={page}&pageSize={pageSize}", cancellationToken);
            }
        }
        
        public async Task<ItemDetailsResponse> GetItemDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            if(id >= 1)
            {
                return await GetJsonAsync<ItemDetailsResponse>($"/api/v1/items/{id}", cancellationToken);
            }
            Logger.LogWarning("Invalid item id {ItemId}. Item id must be greater than 0.", id);
            throw new ArgumentOutOfRangeException(nameof(id), id, "Item id must be greater than 0.");
        }
        
        public async Task<List<ItemListItemResponse>> GetItemsByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(category))
            {
                Logger.LogWarning("Invalid category value {Category}. Category must not be null or whitespace.", category);
                throw new ArgumentException("Category must not be null or whitespace.", nameof(category));
            }

            try
            {
                return await GetJsonAsync<List<ItemListItemResponse>>(
                    $"/api/v1/items/categories/{Uri.EscapeDataString(category)}",
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                if(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.LogWarning("Category {Category} not found.", category);
                    throw new KeyNotFoundException($"Category '{category}' not found.");
                }

                throw;
            }
        }
        
        public Task<List<SyncStateResponse>> GetSyncStatesAsync(CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<SyncStateResponse>>("/api/v1/items/sync", cancellationToken);
        
        public Task<List<SyncStateResponse>> GetSyncStatesByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default) =>
            GetJsonAsync<List<SyncStateResponse>>(
                $"/api/v1/items/sync/by-date?time={Uri.EscapeDataString(date.ToString("O", CultureInfo.InvariantCulture))}",
                cancellationToken);
    }
}
