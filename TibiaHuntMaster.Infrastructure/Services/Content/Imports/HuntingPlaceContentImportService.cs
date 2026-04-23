using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Content.HuntingPlaces;
using TibiaHuntMaster.Core.Content.Sync;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Http.Content.Abstractions;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Mapping;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Imports
{
    internal sealed class HuntingPlaceContentImportService(
        IHuntingPlacesClient huntingPlacesClient,
        IDbContextFactory<AppDbContext> dbFactory,
        IContentProgressService progressService,
        ILogger<HuntingPlaceContentImportService> logger) : IHuntingPlaceContentImportService
    {
        
        private const int MaxParallelRequests = 5;
        
        public async Task<ContentOperationResult> ImportHuntingPlacesAsync(CancellationToken ct = default)
        {
            progressService.Report("Importing hunting places", "Loading hunting place index...", 75);
            List<HuntingPlaceListItemResponse> huntingPlaces = await huntingPlacesClient.GetHuntingPlacesAsync(ct);
            progressService.Report("Importing hunting places", $"Loading details for {huntingPlaces.Count} hunting places...", 82);
            HuntingPlaceDetailFetchResult fetchResult = await FetchHuntingPLacesDetailsAsync(huntingPlaces, ct);
            
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            progressService.Report("Importing hunting places", $"Saving {fetchResult.Details.Count} hunting places to the local database...", 90);

            ContentOperationResult upsertResult = await UpsertHuntingPlacesAsync(dbContext, fetchResult.Details, ct);
            ContentOperationResult result = upsertResult with
            {
                Skipped = upsertResult.Skipped + fetchResult.MissingContentIds.Count
            };
            
            logger.LogInformation(
                "Imported hunting places. Loaded: {Loaded}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Failed: {Failed}",
                result.Loaded,
                result.Created,
                result.Updated,
                result.Skipped,
                result.Failed);
            
            return result;
        }

        public async Task<ContentOperationResult> UpdateHuntingPlacesAsync(CancellationToken ct = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            progressService.Report("Refreshing hunting places", "Checking hunting places for changes...", 75);
            
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);
            
            Dictionary<int, HuntingPlaceEntity> localByContentId = await dbContext.HuntingPlaces
                                                                                  .ToDictionaryAsync(x => x.ContentId, ct);

            List<SyncStateResponse> apiSyncState = await huntingPlacesClient.GetSyncStatesAsync(ct);

            List<int> idsToRefresh = [];

            foreach(SyncStateResponse stateResponse in apiSyncState)
            {
                if(!localByContentId.TryGetValue(stateResponse.Id, out HuntingPlaceEntity? localHuntingPlace))
                {
                    idsToRefresh.Add(stateResponse.Id);
                    continue;
                }

                if(stateResponse.LastUpdated > localHuntingPlace.SourceLastUpdatedAt)
                {
                    idsToRefresh.Add(stateResponse.Id);
                }
            }

            if(idsToRefresh.Count == 0)
            {
                stopwatch.Stop();
                progressService.Report("Refreshing hunting places", "Hunting places are already up to date.", 90);
                return new ContentOperationResult(apiSyncState.Count, 0, 0, apiSyncState.Count, 0, stopwatch.Elapsed);
            }

            progressService.Report("Refreshing hunting places", $"Loading {idsToRefresh.Count} changed hunting places...", 84);
            HuntingPlaceDetailFetchResult fetchResult = await FetchHuntingPLacesDetailsAsync(idsToRefresh, ct);

            int removed = 0;
            if(fetchResult.MissingContentIds.Count > 0)
            {
                List<HuntingPlaceEntity> removedPlaces = localByContentId
                    .Where(entry => fetchResult.MissingContentIds.Contains(entry.Key))
                    .Select(entry => entry.Value)
                    .ToList();

                if(removedPlaces.Count > 0)
                {
                    dbContext.HuntingPlaces.RemoveRange(removedPlaces);
                    removed = removedPlaces.Count;

                    foreach(HuntingPlaceEntity removedPlace in removedPlaces)
                    {
                        localByContentId.Remove(removedPlace.ContentId);
                    }
                }
            }

            progressService.Report("Refreshing hunting places", $"Saving {fetchResult.Details.Count} changed hunting places...", 90);
            ContentOperationResult upsertResult = await UpsertHuntingPlacesAsync(dbContext, fetchResult.Details, ct);
            int skippedMissing = Math.Max(0, fetchResult.MissingContentIds.Count - removed);

            return upsertResult with
            {
                Updated = upsertResult.Updated + removed,
                Skipped = upsertResult.Skipped + skippedMissing
            };
        }
        
        public async Task<ContentOperationResult> ReInitializeHuntingPlacesAsync(CancellationToken ct = default)
        {
            await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);

            dbContext.HuntingPlaceCreatures.RemoveRange(dbContext.HuntingPlaceCreatures);
            dbContext.HuntingPlaceLevels.RemoveRange(dbContext.HuntingPlaceLevels);
            dbContext.HuntingPlaces.RemoveRange(dbContext.HuntingPlaces);
            await dbContext.SaveChangesAsync(ct);
            
            return await ImportHuntingPlacesAsync(ct);
        }
        
        private async Task<HuntingPlaceDetailFetchResult> FetchHuntingPLacesDetailsAsync(
            List<HuntingPlaceListItemResponse> huntingPlaces,
            CancellationToken ct)
        {
            ConcurrentBag<HuntingPlaceDetailsResponse> huntingPlaceDetails = [];
            ConcurrentBag<int> missingContentIds = [];
            int processed = 0;
            int total = huntingPlaces.Count;
            
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };
            
            await Parallel.ForEachAsync(huntingPlaces, options, async (huntingPlace, token) =>
            {
                try
                {
                    HuntingPlaceDetailsResponse details = await huntingPlacesClient.GetHuntingPlaceDetailsAsync(huntingPlace.Id, token);
                    huntingPlaceDetails.Add(details);
                }
                catch(HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    missingContentIds.Add(huntingPlace.Id);
                    logger.LogWarning("Skipping hunting place {ContentId} during import because the API returned 404.", huntingPlace.Id);
                }

                ReportDetailProgress(total, Interlocked.Increment(ref processed));
            });
            
            return new HuntingPlaceDetailFetchResult(
                huntingPlaceDetails.OrderBy(x => x.Id).ToList(),
                missingContentIds.Distinct().OrderBy(id => id).ToList());
        }
        
        private async Task<HuntingPlaceDetailFetchResult> FetchHuntingPLacesDetailsAsync(
            List<int> huntingPlacesId,
            CancellationToken ct)
        
        {
            ConcurrentBag<HuntingPlaceDetailsResponse> huntingPlaceDetails = [];
            ConcurrentBag<int> missingContentIds = [];
            int processed = 0;
            int total = huntingPlacesId.Count;
            
            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = MaxParallelRequests,
                CancellationToken = ct
            };
            
            await Parallel.ForEachAsync(huntingPlacesId, options, async (id, token) =>
            {
                try
                {
                    HuntingPlaceDetailsResponse details = await huntingPlacesClient.GetHuntingPlaceDetailsAsync(id, token);
                    huntingPlaceDetails.Add(details);
                }
                catch(HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    missingContentIds.Add(id);
                    logger.LogWarning("Removing stale local hunting place {ContentId} because the API returned 404 during refresh.", id);
                }

                ReportDetailProgress(total, Interlocked.Increment(ref processed));
            });
            
            return new HuntingPlaceDetailFetchResult(
                huntingPlaceDetails.OrderBy(x => x.Id).ToList(),
                missingContentIds.Distinct().OrderBy(id => id).ToList());
        }

        private async Task<ContentOperationResult> UpsertHuntingPlacesAsync(
            AppDbContext dbContext,
            List<HuntingPlaceDetailsResponse> huntingPlaceDetails,
            CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<int> contentIds = huntingPlaceDetails
                                   .Select(details => details.Id)
                                   .Distinct()
                                   .ToList();
            
            Dictionary<int, HuntingPlaceEntity> existingByContentId = await dbContext.HuntingPlaces
                                                                                     .Include(huntingPlaces => huntingPlaces.Creatures)
                                                                                     .Include(huntingPlaces => huntingPlaces.LowerLevels)
                                                                                     .Where(huntingPlace => contentIds.Contains(huntingPlace.ContentId))
                                                                                     .ToDictionaryAsync(huntingPlace => huntingPlace.ContentId, ct);

            List<int> referencedCreatureContentIds = huntingPlaceDetails
                                                     .SelectMany(place => place.Creatures)
                                                     .Where(creature => creature.CreatureId.HasValue)
                                                     .Select(creature => creature.CreatureId!.Value)
                                                     .Distinct()
                                                     .ToList();

            Dictionary<int, int> creatureDbIdsByContentId = await dbContext.Creatures
                                                                            .Where(creature => referencedCreatureContentIds.Contains(creature.ContentId))
                                                                            .ToDictionaryAsync(creature => creature.ContentId, creature => creature.Id, ct);

            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach(HuntingPlaceDetailsResponse huntingPlaceDetail in huntingPlaceDetails)
            {
                ct.ThrowIfCancellationRequested();

                string contentHash = HuntingPlaceContentMapper.ComputeContentHash(huntingPlaceDetail);

                if(!existingByContentId.TryGetValue(huntingPlaceDetail.Id, out HuntingPlaceEntity? entity))
                {
                    entity = new HuntingPlaceEntity();
                    HuntingPlaceContentMapper.Apply(entity, huntingPlaceDetail, contentHash);
                    ResolveCreatureLinks(entity, creatureDbIdsByContentId);
                    dbContext.HuntingPlaces.Add(entity);
                    existingByContentId[huntingPlaceDetail.Id] = entity;
                    created++;
                    continue;
                }

                if(string.Equals(entity.ContentHash, contentHash, StringComparison.Ordinal))
                {
                    skipped++;
                    continue;
                }
                
                HuntingPlaceContentMapper.Apply(entity, huntingPlaceDetail, contentHash);
                ResolveCreatureLinks(entity, creatureDbIdsByContentId);
                updated++;
            }
            
            await dbContext.SaveChangesAsync(ct);
            stopwatch.Stop();
            
            return new ContentOperationResult(huntingPlaceDetails.Count, created, updated, skipped, 0, stopwatch.Elapsed);
        }

        private static void ResolveCreatureLinks(HuntingPlaceEntity entity, IReadOnlyDictionary<int, int> creatureDbIdsByContentId)
        {
            foreach(HuntingPlaceCreatureEntity creature in entity.Creatures)
            {
                if(!creature.CreatureId.HasValue)
                {
                    continue;
                }

                creature.CreatureId = creatureDbIdsByContentId.TryGetValue(creature.CreatureId.Value, out int dbCreatureId)
                    ? dbCreatureId
                    : null;
            }
        }

        private void ReportDetailProgress(int total, int processed)
        {
            if(processed != 1 && processed != total && processed % 50 != 0)
            {
                return;
            }

            double progress = 82 + (processed / (double)Math.Max(total, 1) * 8);
            progressService.Report("Importing hunting places", $"Loaded hunting place details {processed}/{total}...", progress);
        }

        private sealed record HuntingPlaceDetailFetchResult(
            List<HuntingPlaceDetailsResponse> Details,
            List<int> MissingContentIds);
    }
}
