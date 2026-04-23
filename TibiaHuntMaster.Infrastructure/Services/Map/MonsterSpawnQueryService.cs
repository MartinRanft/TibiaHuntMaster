using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Core.Abstractions.Map;
using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.Infrastructure.Services.Map
{
    public sealed class MonsterSpawnQueryService(IDbContextFactory<AppDbContext> dbFactory) : IMonsterSpawnQueryService
    {
        public async Task<IReadOnlyList<Core.Map.Map.MonsterSpawnMarker>> GetSpawnsInBoundsAsync(
            int minX,
            int minY,
            int maxX,
            int maxY,
            byte z,
            string? monsterName = null,
            int? maxResults = null,
            CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<MonsterSpawnCreatureLinkEntity> query = db.MonsterSpawnCreatureLinks
                                                                 .AsNoTracking()
                                                                 .Where(link => link.Coordinate.Z == z &&
                                                                                link.Coordinate.X >= minX &&
                                                                                link.Coordinate.X <= maxX &&
                                                                                link.Coordinate.Y >= minY &&
                                                                                link.Coordinate.Y <= maxY);

            if (!string.IsNullOrWhiteSpace(monsterName))
            {
                string normalized = monsterName.Trim();
                query = query.Where(link => EF.Functions.Like(link.MonsterName, $"%{normalized}%"));
            }

            if (maxResults.HasValue && maxResults.Value > 0)
            {
                int centerX = minX + ((maxX - minX) / 2);
                int centerY = minY + ((maxY - minY) / 2);

                query = query.OrderBy(link =>
                                  Math.Abs(link.Coordinate.X - centerX) +
                                  Math.Abs(link.Coordinate.Y - centerY))
                             .ThenBy(link => link.Coordinate.X)
                             .ThenBy(link => link.Coordinate.Y)
                             .Take(maxResults.Value);
            }
            else
            {
                query = query.OrderBy(link => link.Coordinate.X)
                             .ThenBy(link => link.Coordinate.Y);
            }

            List<Core.Map.Map.MonsterSpawnMarker> markers = await query
                                                                 .Select(link => new Core.Map.Map.MonsterSpawnMarker(
                                                                     link.Coordinate.X,
                                                                     link.Coordinate.Y,
                                                                     link.Coordinate.Z,
                                                                     link.MonsterName,
                                                                     link.CreatureId,
                                                                     link.SpawnTimeSeconds,
                                                                     link.Direction))
                                                                 .ToListAsync(ct);

            return markers;
        }

        public async Task<IReadOnlyList<string>> SearchMonsterNamesAsync(
            string query,
            int limit = 12,
            CancellationToken ct = default)
        {
            if (limit <= 0 || string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<string>();
            }

            string normalized = query.Trim().ToLowerInvariant();
            if (normalized.Length == 0)
            {
                return Array.Empty<string>();
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<string> names = db.MonsterSpawnCreatureLinks
                                         .AsNoTracking()
                                         .Select(link => link.MonsterName);

            List<string> prefixMatches = await names
                                              .Where(name => name.ToLower().StartsWith(normalized))
                                              .Distinct()
                                              .OrderBy(name => name)
                                              .Take(limit)
                                              .ToListAsync(ct);

            if (prefixMatches.Count >= limit)
            {
                return prefixMatches;
            }

            int remaining = limit - prefixMatches.Count;
            HashSet<string> existing = new(prefixMatches, StringComparer.Ordinal);

            List<string> containsMatches = await names
                                               .Where(name => name.ToLower().Contains(normalized))
                                               .Distinct()
                                               .OrderBy(name => name)
                                               .ToListAsync(ct);

            List<string> result = new(prefixMatches);
            foreach (string name in containsMatches)
            {
                if (!existing.Add(name))
                {
                    continue;
                }

                result.Add(name);
                if (result.Count >= limit)
                {
                    break;
                }
            }

            return result;
        }
    }
}
