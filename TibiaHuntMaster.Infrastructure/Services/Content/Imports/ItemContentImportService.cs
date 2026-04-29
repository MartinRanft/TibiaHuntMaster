using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.Items;
using TibiaHuntMaster.Core.Content.Shared;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Mapping;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Imports
{
    internal sealed class ItemContentImportService(
        IItemsClient itemsClient,
        IDbContextFactory<AppDbContext> dbFactory,
        IContentProgressService progressService,
        ILogger<ItemContentImportService> logger) : IItemContentImportService
    {
        private const int MaxParallelRequests = 5;

        public async Task<ContentOperationResult> ImportItemsAsync(CancellationToken ct = default)
        {
            progressService.Report("Importing items", "Loading item index...", 15);
            PagedResponseOfItemListItemResponse firstPage = await itemsClient.GetPagedItemAsync(1, 100, ct);
            int itemPages = (firstPage.TotalCount + firstPage.PageSize - 1) / firstPage.PageSize;

            List<ItemListItemResponse> allItems = await FetchItemsInParallelAsync(firstPage, itemPages, ct);
            progressService.Report("Importing items", $"Loading item details for {allItems.Count} items...", 22);
            List<ItemDetailsResponse> itemDetails = await FetchItemDetailsAsync(allItems, ct);

            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            progressService.Report("Importing items", $"Saving {itemDetails.Count} items to the local database...", 34);
            ContentOperationResult result = await UpsertItemsAsync(dbContext, itemDetails, ct);

            logger.LogInformation(
                "Imported items. Loaded: {Loaded}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}",
                result.Loaded,
                result.Created,
                result.Updated,
                result.Skipped,
                result.Failed);

            return result;
        }

        public async Task<ContentOperationResult> UpdateItemsAsync(CancellationToken ct = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            progressService.Report("Refreshing items", "Checking items for changes...", 15);
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);

            Dictionary<int, ItemEntity> localByContentId = await dbContext.Items
                                                                          .ToDictionaryAsync(x => x.ContentId, ct);

            List<SyncStateResponse> apiSyncStates = await itemsClient.GetSyncStatesAsync(ct);

            List<int> idsToRefresh = [];

            foreach(SyncStateResponse apiState in apiSyncStates)
            {
                if(!localByContentId.TryGetValue(apiState.Id, out ItemEntity? localItem))
                {
                    idsToRefresh.Add(apiState.Id);
                    continue;
                }

                if(apiState.LastUpdated > localItem.SourceLastUpdatedAt)
                {
                    idsToRefresh.Add(apiState.Id);
                }
            }

            if(idsToRefresh.Count == 0)
            {
                stopwatch.Stop();
                progressService.Report("Refreshing items", "Items are already up to date.", 34);
                return new ContentOperationResult(apiSyncStates.Count, 0, 0, apiSyncStates.Count, 0, stopwatch.Elapsed);
            }

            progressService.Report("Refreshing items", $"Loading {idsToRefresh.Count} changed items...", 24);
            List<ItemDetailsResponse> changedItems = await FetchItemDetailsAsync(idsToRefresh, ct);
            progressService.Report("Refreshing items", $"Saving {changedItems.Count} changed items...", 34);
            
            return await UpsertItemsAsync(dbContext, changedItems, ct);
        }

        public async Task<ContentOperationResult> ReInitializeItemsAsync(CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            
            dbContext.Items.RemoveRange(dbContext.Items);
            await dbContext.SaveChangesAsync(ct);

            return await ImportItemsAsync(ct);
        }

        private async Task<List<ItemListItemResponse>> FetchItemsInParallelAsync(
            PagedResponseOfItemListItemResponse firstPage,
            int itemPages,
            CancellationToken ct = default)
        {
            ConcurrentBag<ItemListItemResponse> allItems = [];

            foreach(ItemListItemResponse item in firstPage.Items)
            {
                allItems.Add(item);
            }

            if(itemPages <= 1)
            {
                return allItems.OrderBy(x => x.Id).ToList();
            }

            List<int> remainingPages = Enumerable.Range(2, itemPages - 1).ToList();

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(remainingPages, options, async (page, token) =>
            {
                PagedResponseOfItemListItemResponse response = await itemsClient.GetPagedItemAsync(page, firstPage.PageSize, token);

                foreach(ItemListItemResponse item in response.Items)
                {
                    allItems.Add(item);
                }
            });

            return allItems.OrderBy(x => x.Id).ToList();
        }

        private async Task<List<ItemDetailsResponse>> FetchItemDetailsAsync(List<ItemListItemResponse> allItems, CancellationToken ct = default)
        {
            ConcurrentBag<ItemDetailsResponse> itemDetails = [];
            int processed = 0;
            int total = allItems.Count;

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(allItems, options, async (item, token) =>
            {
                ItemDetailsResponse details = await itemsClient.GetItemDetailsAsync(item.Id, token);
                itemDetails.Add(details);
                ReportDetailProgress(total, Interlocked.Increment(ref processed));
            });

            return itemDetails.OrderBy(x => x.Id).ToList();
        }

        private async Task<List<ItemDetailsResponse>> FetchItemDetailsAsync(List<int> itemIds, CancellationToken ct = default)
        {
            ConcurrentBag<ItemDetailsResponse> itemDetails = [];
            int processed = 0;
            int total = itemIds.Count;

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(itemIds, options, async (itemId, token) =>
            {
                try
                {
                    ItemDetailsResponse details = await itemsClient.GetItemDetailsAsync(itemId, token);
                    itemDetails.Add(details);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    // Item no longer exists in the content API — skip silently
                }
                finally
                {
                    ReportDetailProgress(total, Interlocked.Increment(ref processed));
                }
            });

            return itemDetails.OrderBy(x => x.Id).ToList();
        }

        private static async Task<ContentOperationResult> UpsertItemsAsync(
            AppDbContext dbContext,
            IReadOnlyList<ItemDetailsResponse> itemDetails,
            CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<int> contentIds = itemDetails
                .Select(detail => detail.Id)
                .Distinct()
                .ToList();

            Dictionary<int, ItemEntity> existingByContentId = await dbContext.Items
                .Where(item => contentIds.Contains(item.ContentId))
                .ToDictionaryAsync(item => item.ContentId, ct);

            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach(ItemDetailsResponse detail in itemDetails)
            {
                ct.ThrowIfCancellationRequested();

                string contentHash = ItemContentMapper.ComputeContentHash(detail);

                if(!existingByContentId.TryGetValue(detail.Id, out ItemEntity? entity))
                {
                    entity = new ItemEntity
                    {
                        ContentId = detail.Id,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    };

                    ItemContentMapper.Apply(entity, detail, contentHash);
                    dbContext.Items.Add(entity);
                    existingByContentId[detail.Id] = entity;
                    created++;
                    continue;
                }

                if(string.Equals(entity.ContentHash, contentHash, StringComparison.Ordinal))
                {
                    skipped++;
                    continue;
                }

                ItemContentMapper.Apply(entity, detail, contentHash);
                updated++;
            }

            await dbContext.SaveChangesAsync(ct);
            stopwatch.Stop();

            return new ContentOperationResult(itemDetails.Count, created, updated, skipped, 0, stopwatch.Elapsed);
        }

        private void ReportDetailProgress(int total, int processed)
        {
            if(processed != 1 && processed != total && processed % 250 != 0)
            {
                return;
            }

            double progress = 22 + (processed / (double)Math.Max(total, 1) * 10);
            progressService.Report("Importing items", $"Loaded item details {processed}/{total}...", progress);
        }
    }
}
