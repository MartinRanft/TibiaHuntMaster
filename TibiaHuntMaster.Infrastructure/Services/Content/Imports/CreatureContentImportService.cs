using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.Creatures;
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
     internal sealed class CreatureContentImportService(
        ICreaturesClient creaturesClient,
        IDbContextFactory<AppDbContext> dbFactory,
        IContentProgressService progressService,
        ILogger<CreatureContentImportService> logger) : ICreatureContentImportService
    {
        
        private const int MaxParallelRequests = 5;
        
        public async Task<ContentOperationResult> ImportCreaturesAsync(CancellationToken ct = default)
        {
            progressService.Report("Importing creatures", "Loading creature index...", 45);
            PagedResponseOfCreatureListItemResponse firstPage = await creaturesClient.GetPagedCreatureAsync(1, 100, ct);
            int creaturePages = (firstPage.TotalCount + firstPage.PageSize - 1) / firstPage.PageSize;
            
            List<CreatureListItemResponse> allCreatures = await FetchCreaturesInParallelAsync(firstPage, creaturePages, ct);
            progressService.Report("Importing creatures", $"Loading creature details for {allCreatures.Count} creatures...", 52);
            List<CreatureDetailsResponse> creatureDetails = await FetchCreatureDetailsAsync(allCreatures, ct);

            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            progressService.Report("Importing creatures", $"Saving {creatureDetails.Count} creatures to the local database...", 64);

            ContentOperationResult result = await UpsertCreaturesAsync(dbContext, creatureDetails, ct);
            
            logger.LogInformation(
                "Imported creatures. Loaded: {Loaded}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}",
                result.Loaded,
                result.Created,
                result.Updated,
                result.Skipped,
                result.Failed);
            
            return result;
        }

        public async Task<ContentOperationResult> UpdateCreaturesAsync(CancellationToken ct = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            progressService.Report("Refreshing creatures", "Checking creatures for changes...", 45);
            
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            
            Dictionary<int, CreatureEntity> localByContentId = await dbContext.Creatures
                                                                          .ToDictionaryAsync(x => x.ContentId, ct);

            List<SyncStateResponse> apiSyncStates = await creaturesClient.GetSyncStatesAsync(ct);

            List<int> idsToRefresh = [];

            foreach(SyncStateResponse apiState in apiSyncStates)
            {
                if(!localByContentId.TryGetValue(apiState.Id, out CreatureEntity? localCreature))
                {
                    idsToRefresh.Add(apiState.Id);
                    continue;
                }

                if(apiState.LastUpdated > localCreature.SourceLastUpdatedAt)
                {
                    idsToRefresh.Add(apiState.Id);
                }
            }

            if(idsToRefresh.Count == 0)
            {
                stopwatch.Stop();
                progressService.Report("Refreshing creatures", "Creatures are already up to date.", 64);
                return new ContentOperationResult(apiSyncStates.Count, 0, 0, apiSyncStates.Count, 0, stopwatch.Elapsed);
            }

            progressService.Report("Refreshing creatures", $"Loading {idsToRefresh.Count} changed creatures...", 54);
            List<CreatureDetailsResponse> changedCreatures = await FetchCreatureDetailsAsync(idsToRefresh, ct);
            progressService.Report("Refreshing creatures", $"Saving {changedCreatures.Count} changed creatures...", 64);
            
            return await UpsertCreaturesAsync(dbContext, changedCreatures, ct);
        }
        
        public async Task<ContentOperationResult> ReInitializeCreaturesAsync(CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            
            dbContext.Creatures.RemoveRange(dbContext.Creatures);
            await dbContext.SaveChangesAsync(ct);

            return await ImportCreaturesAsync(ct);
        }
        
        private async Task<List<CreatureListItemResponse>> FetchCreaturesInParallelAsync(
            PagedResponseOfCreatureListItemResponse firstPage, 
            int creaturePages, CancellationToken ct)
        {
            ConcurrentBag<CreatureListItemResponse> allCreatures = [];

            foreach(CreatureListItemResponse creature in firstPage.Creatures)
            {
                allCreatures.Add(creature);
            }

            if(creaturePages <= 1)
            {
                return allCreatures.OrderBy(x => x.Id).ToList();
            }
            
            List<int> remainingPages = Enumerable.Range(2, creaturePages - 1).ToList();
            
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(remainingPages,
                options,
                async (page, token) =>
                {
                    PagedResponseOfCreatureListItemResponse response = await creaturesClient.GetPagedCreatureAsync(page, firstPage.PageSize, token);

                    foreach(CreatureListItemResponse creature in response.Creatures)
                    {
                        allCreatures.Add(creature);
                    }
                });
            
            return allCreatures.OrderBy(x => x.Id).ToList();
        }
        
        private async Task<List<CreatureDetailsResponse>> FetchCreatureDetailsAsync(
            List<CreatureListItemResponse> allCreatures,
            CancellationToken ct)
        {
            ConcurrentBag<CreatureDetailsResponse> creatureDetails = [];
            int processed = 0;
            int total = allCreatures.Count;

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(allCreatures,
                options,
                async (creature, token) =>
                {
                    CreatureDetailsResponse details = await creaturesClient.GetCreatureDetailsAsync(creature.Id, token);
                    creatureDetails.Add(details);
                    ReportDetailProgress(total, Interlocked.Increment(ref processed));
                });
            return creatureDetails.OrderBy(x => x.Id).ToList();
        }

        private async Task<List<CreatureDetailsResponse>> FetchCreatureDetailsAsync(
            List<int> creatureIds,
            CancellationToken ct)
        {
            ConcurrentBag<CreatureDetailsResponse> creatureDetails = [];
            int processed = 0;
            int total = creatureIds.Count;

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(creatureIds,
                options,
                async (creatureId, token) =>
                {
                    CreatureDetailsResponse details = await creaturesClient.GetCreatureDetailsAsync(creatureId, token);
                    creatureDetails.Add(details);
                    ReportDetailProgress(total, Interlocked.Increment(ref processed));
                });

            return creatureDetails.OrderBy(x => x.Id).ToList();
        }
        
        private async Task<ContentOperationResult> UpsertCreaturesAsync(
            AppDbContext dbContext,
            IReadOnlyList<CreatureDetailsResponse> creatureDetails,
            CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<int> contentIds = creatureDetails
                                   .Select(details => details.Id)
                                   .Distinct()
                                   .ToList();
            
            Dictionary<int, CreatureEntity> existingByContentId = await dbContext.Creatures
                                                                                  .Include(creature => creature.Loot)
                                                                                  .Include(creature => creature.Sounds)
                                                                                  .Where(item => contentIds.Contains(item.ContentId))
                                                                                  .ToDictionaryAsync(item => item.ContentId, ct);

            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach(CreatureDetailsResponse detail in creatureDetails)
            {
                ct.ThrowIfCancellationRequested();

                string contentHash = CreatureContentMapper.ComputeContentHash(detail);

                if(!existingByContentId.TryGetValue(detail.Id, out CreatureEntity? entity))
                {
                    entity = new CreatureEntity();
                    CreatureContentMapper.Apply(entity, detail, contentHash);
                    dbContext.Creatures.Add(entity);
                    existingByContentId[detail.Id] = entity;
                    created++;
                    continue;
                }

                if(string.Equals(entity.ContentHash, contentHash, StringComparison.Ordinal))
                {
                    skipped++;
                    continue;
                }

                CreatureContentMapper.Apply(entity, detail, contentHash);
                updated++;
            }

            await dbContext.SaveChangesAsync(ct);
            stopwatch.Stop();

            return new ContentOperationResult(creatureDetails.Count, created, updated, skipped, 0, stopwatch.Elapsed);
        }

        private void ReportDetailProgress(int total, int processed)
        {
            if(processed != 1 && processed != total && processed % 100 != 0)
            {
                return;
            }

            double progress = 52 + (processed / (double)Math.Max(total, 1) * 10);
            progressService.Report("Importing creatures", $"Loaded creature details {processed}/{total}...", progress);
        }
    }
}
