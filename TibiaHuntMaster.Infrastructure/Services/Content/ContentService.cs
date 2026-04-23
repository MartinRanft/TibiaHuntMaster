using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Content.Imports.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content
{
    public sealed class ContentService(
        IDbContextFactory<AppDbContext> dbFactory,
        IItemContentImportService itemImportService,
        ICreatureContentImportService creatureImportService,
        IHuntingPlaceContentImportService huntingPlaceImportService,
        IContentProgressService progressService,
        ILogger<ContentService> logger) : IContentService
    {

        public async Task<ContentInitializationResult> EnsureContentInitializedAsync(CancellationToken ct = default)
        {
            {
                await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);

                bool hasItems = await dbContext.Items.AnyAsync(ct);
                bool hasCreatures = await dbContext.Creatures.AnyAsync(ct);
                bool hasHuntingPlaces = await dbContext.HuntingPlaces.AnyAsync(ct);

                if(hasItems && hasCreatures && hasHuntingPlaces)
                {
                    progressService.Report("Checking local content", "Existing content found. Initial import is not required.", 25);
                    ContentInitializationResult notRequired = ContentInitializationResult.NotRequired();
                    logger.LogInformation("Content initialization not required: {Result}", notRequired);
                    return notRequired;
                }
            }

            progressService.Report("Importing items", "Starting item import...", 12);
            ContentOperationResult generatedItems = await itemImportService.ImportItemsAsync(ct);
            progressService.Report("Importing creatures", "Starting creature import...", 42);
            ContentOperationResult generatedCreatures = await creatureImportService.ImportCreaturesAsync(ct);
            progressService.Report("Importing hunting places", "Starting hunting place import...", 72);
            ContentOperationResult generatedHuntingPlaces = await huntingPlaceImportService.ImportHuntingPlacesAsync(ct);
            
            ContentInitializationResult result = new(true, generatedItems, generatedCreatures, generatedHuntingPlaces);
            logger.LogInformation("Content initialization result: {Result}", result);
            return result;
        }

        public async Task<ContentRefreshResult> RefreshContentAsync(CancellationToken ct = default)
        {
            progressService.Report("Refreshing items", "Checking items for updates...", 18);
            ContentOperationResult updatedItems = await itemImportService.UpdateItemsAsync(ct);
            progressService.Report("Refreshing creatures", "Checking creatures for updates...", 48);
            ContentOperationResult updatedCreatures = await creatureImportService.UpdateCreaturesAsync(ct);
            progressService.Report("Refreshing hunting places", "Checking hunting places for updates...", 78);
            ContentOperationResult updatedHuntingPlaces = await huntingPlaceImportService.UpdateHuntingPlacesAsync(ct);

            ContentRefreshResult result = new(updatedItems, updatedCreatures, updatedHuntingPlaces);
            logger.LogInformation("Content refresh result: {Result}", result);
            
            return result;
        }

        public async Task<ContentReInitializationResult> ReInitializeContentAsync(CancellationToken ct = default)
        {
            progressService.Report("Rebuilding local content", "Clearing existing content tables...", 5);
            {
                await using AppDbContext dbContext = await dbFactory.CreateDbContextAsync(ct);

                dbContext.HuntingPlaceCreatures.RemoveRange(dbContext.HuntingPlaceCreatures);
                dbContext.HuntingPlaceLevels.RemoveRange(dbContext.HuntingPlaceLevels);
                dbContext.HuntingPlaces.RemoveRange(dbContext.HuntingPlaces);
                dbContext.CreatureLoots.RemoveRange(dbContext.CreatureLoots);
                dbContext.CreatureSounds.RemoveRange(dbContext.CreatureSounds);
                dbContext.Creatures.RemoveRange(dbContext.Creatures);
                dbContext.Items.RemoveRange(dbContext.Items);
                await dbContext.SaveChangesAsync(ct);
            }

            progressService.Report("Importing items", "Re-importing items...", 12);
            ContentOperationResult updatedItems = await itemImportService.ImportItemsAsync(ct);
            progressService.Report("Importing creatures", "Re-importing creatures...", 42);
            ContentOperationResult updatedCreatures = await creatureImportService.ImportCreaturesAsync(ct);
            progressService.Report("Importing hunting places", "Re-importing hunting places...", 72);
            ContentOperationResult updatedHuntingPlaces = await huntingPlaceImportService.ImportHuntingPlacesAsync(ct);

            ContentReInitializationResult result = new(updatedItems, updatedCreatures, updatedHuntingPlaces);
            logger.LogInformation("Content re-initialization result: {Result}", result);
            
            return result;
        }
    }
}
