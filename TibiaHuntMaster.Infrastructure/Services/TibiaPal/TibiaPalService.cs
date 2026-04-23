using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Core.TibiaPal;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Http.TibiaPal;

namespace TibiaHuntMaster.Infrastructure.Services.TibiaPal
{
    public sealed class TibiaPalService(TibiaPalClient client, IDbContextFactory<AppDbContext> dbFactory)
    {
        public async Task<List<EnrichedHuntingSpot>> GetRecommendationsAsync(string vocation, int currentLevel, CancellationToken ct = default)
        {
            List<TibiaPalHuntingSpot> rawSpots = await client.FetchHuntingSpotsAsync(vocation, ct);

            int minLevelThreshold = (int)(currentLevel * 0.8);
            List<TibiaPalHuntingSpot> relevantSpots = rawSpots.Where(s => s.MinLevel >= minLevelThreshold).ToList();

            // DB Zugriff über Factory
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            var dbPlaces = await db.HuntingPlaces
                                   .Select(x => new
                                   {
                                       x.Id,
                                       x.Name,
                                       x.Image
                                   })
                                   .AsNoTracking()
                                   .ToListAsync(ct);

            List<EnrichedHuntingSpot> result = [];
            result.AddRange(
                from spot in relevantSpots
                let dbMatch = dbPlaces.FirstOrDefault(dbP =>
                dbP.Name.Contains(spot.Name, StringComparison.OrdinalIgnoreCase) ||
                spot.Name.Contains(dbP.Name, StringComparison.OrdinalIgnoreCase))
                select new EnrichedHuntingSpot(
                    spot,
                    dbMatch?.Id,
                    dbMatch?.Name,
                    dbMatch?.Image,
                    dbMatch != null
                )
            );

            return result.OrderBy(x => x.TibiaPalData.MinLevel).ToList();
        }
    }

    public sealed record EnrichedHuntingSpot(
        TibiaPalHuntingSpot TibiaPalData,
        int? DbId,
        string? DbName,
        string? ImageUrl,
        bool HasWikiData
    );
}