using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed class GlossarySearchService(IDbContextFactory<AppDbContext> dbFactory) : IGlossarySearchService
    {
        public async Task<List<CreatureEntitys>> FindCreaturesDroppingAsync(string itemNameFragment, CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            List<int> creatureIds = await db.CreatureLoots
                                            .Where(l => EF.Functions.Like(l.ItemName, $"%{itemNameFragment}%"))
                                            .Select(l => l.CreatureId)
                                            .Distinct()
                                            .ToListAsync(ct);

            if(creatureIds.Count == 0)
            {
                return [];
            }

            return await db.Creatures
                           .Where(c => creatureIds.Contains(c.Id))
                           .OrderBy(c => c.Name)
                           .ToListAsync(ct);
        }

        public async Task<List<ItemEntity>> SearchItemsAsync(string query, CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            string normalizedQuery = query.Trim().ToUpperInvariant();

            List<ItemEntity> results = await db.Items
                                               .Where(i => i.NormalizedName.StartsWith(normalizedQuery))
                                               .Take(50)
                                               .OrderBy(i => i.Name)
                                               .ToListAsync(ct);

            if(results.Count < 10)
            {
                List<int> excludeIds = results.Select(r => r.Id).ToList();

                List<ItemEntity> fuzzyResults = await db.Items
                                                        .Where(i => !excludeIds.Contains(i.Id) && EF.Functions.Like(i.NormalizedName, $"%{normalizedQuery}%"))
                                                        .Take(50 - results.Count)
                                                        .OrderBy(i => i.Name)
                                                        .ToListAsync(ct);

                results.AddRange(fuzzyResults);
            }

            return results;
        }
    }
}
