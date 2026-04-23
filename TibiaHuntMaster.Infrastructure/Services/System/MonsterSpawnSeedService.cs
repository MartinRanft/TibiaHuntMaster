using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.Infrastructure.Services.System
{
    public sealed class MonsterSpawnSeedService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<MonsterSpawnSeedService> logger,
        Func<string, Stream>? openResourceStream = null)
    {
        private const string SpawnResourceName = "TibiaHuntMaster.Infrastructure.SeedData.Spawns.map-spawn-v2.json.gz";
        private const string MonsterDefinitionsResourceName = "TibiaHuntMaster.Infrastructure.SeedData.Spawns.monsters.json.gz";

        private static readonly IReadOnlyDictionary<string, string[]> NameAliasesBySlug = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["nomadfemale"] = ["nomad"],
            ["oldbear"] = ["bearnostalgia", "bear"],
            ["oldbeholder"] = ["beholdernostalgia", "beholder"],
            ["oldbug"] = ["bugnostalgia", "bug"],
            ["oldgiantspider"] = ["giantspidernostalgia", "giantspider"],
            ["oldpig"] = ["pignostalgia", "pig"],
            ["oldspider"] = ["spidernostalgia", "spider"],
            ["oldwasp"] = ["waspnostalgia", "wasp"],
            ["oldwolf"] = ["wolfnostalgia", "wolf"],
            ["bluebutterfly"] = ["butterflyblue", "butterfly"],
            ["brownhorse"] = ["horsebrown", "horse"],
            ["pinkbutterfly"] = ["butterflypink", "butterfly"],
            ["purplebutterfly"] = ["butterflypurple", "butterfly"],
            ["redbutterfly"] = ["butterflyred", "butterfly"],
            ["yellowbutterfly"] = ["butterflyyellow", "butterfly"]
        };

        private readonly Func<string, Stream> _openResourceStream = openResourceStream ?? OpenEmbeddedResource;

        public async Task EnsureSpawnsSeededAsync(CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            if (await db.MonsterSpawnCoordinates.AnyAsync(ct))
            {
                await ResolveMissingCreatureLinksAsync(db, ct);
                return;
            }

            logger.LogInformation("Seeding monster spawn coordinates from embedded dataset...");

            HashSet<string> knownMonsterNames = LoadMonsterDefinitionNameSlugs();
            List<SpawnSeedRow> spawnRows = LoadSpawnRows();
            Dictionary<string, int> creatureIdsBySlug = await BuildCreatureIdLookupAsync(db, ct);

            int unresolvedCreatureLinks = 0;
            int unknownMonsterNames = 0;

            Dictionary<(int X, int Y, byte Z), MonsterSpawnCoordinateEntity> coordinatesByKey = new(spawnRows.Count);
            List<MonsterSpawnCreatureLinkEntity> linksToInsert = new(spawnRows.Count);
            HashSet<string> dedupe = new(spawnRows.Count, StringComparer.Ordinal);

            foreach (SpawnSeedRow row in spawnRows)
            {
                ct.ThrowIfCancellationRequested();

                if (!knownMonsterNames.Contains(row.MonsterNameSlug))
                {
                    unknownMonsterNames += 1;
                }

                (int X, int Y, byte Z) coordinateKey = (row.X, row.Y, row.Z);
                if (!coordinatesByKey.TryGetValue(coordinateKey, out MonsterSpawnCoordinateEntity? coordinate))
                {
                    coordinate = new MonsterSpawnCoordinateEntity
                    {
                        X = row.X,
                        Y = row.Y,
                        Z = row.Z
                    };

                    coordinatesByKey[coordinateKey] = coordinate;
                }

                string dedupeKey = $"{row.X}|{row.Y}|{row.Z}|{row.MonsterNameSlug}|{row.SpawnTimeSeconds?.ToString(CultureInfo.InvariantCulture) ?? ""}|{row.Direction?.ToString(CultureInfo.InvariantCulture) ?? ""}";
                if (!dedupe.Add(dedupeKey))
                {
                    continue;
                }

                int? creatureId = ResolveCreatureId(row.MonsterName, row.MonsterNameSlug, creatureIdsBySlug);
                if (!creatureId.HasValue)
                {
                    unresolvedCreatureLinks += 1;
                }

                linksToInsert.Add(new MonsterSpawnCreatureLinkEntity
                {
                    Coordinate = coordinate,
                    CreatureId = creatureId,
                    MonsterName = row.MonsterName,
                    SpawnTimeSeconds = row.SpawnTimeSeconds,
                    Direction = row.Direction
                });
            }

            db.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                db.MonsterSpawnCoordinates.AddRange(coordinatesByKey.Values);
                db.MonsterSpawnCreatureLinks.AddRange(linksToInsert);
                await db.SaveChangesAsync(ct);
            }
            finally
            {
                db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            logger.LogInformation(
                "Monster spawn seed completed. Coordinates={Coordinates}, Links={Links}, UnresolvedCreatureLinks={Unresolved}, UnknownMonsterNames={Unknown}.",
                coordinatesByKey.Count,
                linksToInsert.Count,
                unresolvedCreatureLinks,
                unknownMonsterNames);
        }

        private async Task ResolveMissingCreatureLinksAsync(AppDbContext db, CancellationToken ct)
        {
            List<MonsterSpawnCreatureLinkEntity> unresolved = await db.MonsterSpawnCreatureLinks
                                                                      .Where(x => x.CreatureId == null)
                                                                      .ToListAsync(ct);

            if (unresolved.Count == 0)
            {
                return;
            }

            Dictionary<string, int> creatureIdsBySlug = await BuildCreatureIdLookupAsync(db, ct);

            int linked = 0;
            foreach (MonsterSpawnCreatureLinkEntity link in unresolved)
            {
                int? creatureId = ResolveCreatureId(link.MonsterName, ToSlug(link.MonsterName), creatureIdsBySlug);
                if (!creatureId.HasValue)
                {
                    continue;
                }

                link.CreatureId = creatureId.Value;
                linked += 1;
            }

            if (linked > 0)
            {
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Resolved {Resolved} previously unmatched monster spawn links.", linked);
            }
        }

        private static async Task<Dictionary<string, int>> BuildCreatureIdLookupAsync(AppDbContext db, CancellationToken ct)
        {
            Dictionary<string, int> result = new(StringComparer.Ordinal);

            List<(int Id, string Name, string ActualName)> creatures = await db.Creatures
                                                                                .AsNoTracking()
                                                                                .Select(x => new ValueTuple<int, string, string>(x.Id, x.Name, x.ActualName))
                                                                                .ToListAsync(ct);

            foreach ((int id, string name, string actualName) in creatures)
            {
                AddSlug(result, ToSlug(name), id);
                AddSlug(result, ToSlug(actualName), id);
            }

            return result;
        }

        private static void AddSlug(Dictionary<string, int> map, string slug, int creatureId)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return;
            }

            if (!map.ContainsKey(slug))
            {
                map[slug] = creatureId;
            }
        }

        private static int? ResolveCreatureId(string monsterName, string monsterNameSlug, IReadOnlyDictionary<string, int> creatureIdsBySlug)
        {
            IEnumerable<string> candidates = EnumerateSlugCandidates(monsterName, monsterNameSlug);
            foreach (string candidate in candidates)
            {
                if (creatureIdsBySlug.TryGetValue(candidate, out int creatureId))
                {
                    return creatureId;
                }
            }

            return null;
        }

        private static IEnumerable<string> EnumerateSlugCandidates(string monsterName, string monsterNameSlug)
        {
            if (!string.IsNullOrWhiteSpace(monsterNameSlug))
            {
                yield return monsterNameSlug;
            }

            if (NameAliasesBySlug.TryGetValue(monsterNameSlug, out string[]? aliases))
            {
                foreach (string alias in aliases)
                {
                    yield return alias;
                }
            }

            if (monsterName.StartsWith("Old ", StringComparison.OrdinalIgnoreCase))
            {
                string withoutOld = monsterName[4..];
                string withoutOldSlug = ToSlug(withoutOld);
                if (!string.IsNullOrWhiteSpace(withoutOldSlug))
                {
                    yield return withoutOldSlug;
                    yield return $"{withoutOldSlug}nostalgia";
                }
            }
        }

        private HashSet<string> LoadMonsterDefinitionNameSlugs()
        {
            HashSet<string> result = new(StringComparer.Ordinal);

            using Stream baseStream = _openResourceStream(MonsterDefinitionsResourceName);
            using GZipStream gzip = new(baseStream, CompressionMode.Decompress);
            using JsonDocument document = JsonDocument.Parse(gzip);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (JsonElement monster in document.RootElement.EnumerateArray())
            {
                if (!TryGetString(monster, "name", out string? name))
                {
                    continue;
                }

                string slug = ToSlug(name);
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    result.Add(slug);
                }
            }

            return result;
        }

        private List<SpawnSeedRow> LoadSpawnRows()
        {
            List<SpawnSeedRow> rows = new(capacity: 90000);

            using Stream baseStream = _openResourceStream(SpawnResourceName);
            using GZipStream gzip = new(baseStream, CompressionMode.Decompress);
            using JsonDocument document = JsonDocument.Parse(gzip);

            if (!document.RootElement.TryGetProperty("spawns", out JsonElement spawns) || spawns.ValueKind != JsonValueKind.Array)
            {
                return rows;
            }

            foreach (JsonElement spawn in spawns.EnumerateArray())
            {
                if (!TryGetInt(spawn, "centerx", out int centerX) ||
                    !TryGetInt(spawn, "centery", out int centerY) ||
                    !TryGetInt(spawn, "centerz", out int centerZ))
                {
                    continue;
                }

                if (!spawn.TryGetProperty("monsters", out JsonElement monsters) || monsters.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement monster in monsters.EnumerateArray())
                {
                    if (!TryGetString(monster, "name", out string? monsterName))
                    {
                        continue;
                    }

                    if (!TryGetInt(monster, "x", out int offsetX) ||
                        !TryGetInt(monster, "y", out int offsetY))
                    {
                        continue;
                    }

                    int zValue = centerZ;
                    if (TryGetInt(monster, "z", out int parsedMonsterZ))
                    {
                        zValue = parsedMonsterZ;
                    }

                    if (zValue < 0 || zValue > byte.MaxValue)
                    {
                        continue;
                    }

                    int absoluteX = centerX + offsetX;
                    int absoluteY = centerY + offsetY;
                    byte absoluteZ = (byte)zValue;

                    int? spawnTime = null;
                    if (TryGetInt(monster, "spawntime", out int parsedSpawnTime) && parsedSpawnTime >= 0)
                    {
                        spawnTime = parsedSpawnTime;
                    }

                    byte? direction = null;
                    if (TryGetInt(monster, "direction", out int parsedDirection) &&
                        parsedDirection >= 0 &&
                        parsedDirection <= byte.MaxValue)
                    {
                        direction = (byte)parsedDirection;
                    }

                    string cleanMonsterName = monsterName!.Trim();

                    rows.Add(new SpawnSeedRow(
                        absoluteX,
                        absoluteY,
                        absoluteZ,
                        cleanMonsterName,
                        ToSlug(cleanMonsterName),
                        spawnTime,
                        direction));
                }
            }

            return rows;
        }

        private static Stream OpenEmbeddedResource(string resourceName)
        {
            Assembly asm = typeof(MonsterSpawnSeedService).Assembly;
            Stream? stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
            }

            return stream;
        }

        private static bool TryGetString(JsonElement element, string propertyName, out string? value)
        {
            value = null;

            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return false;
            }

            if (property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetInt(JsonElement element, string propertyName, out int value)
        {
            value = 0;

            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.TryGetInt32(out value);
            }

            if (property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            string? raw = property.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string ToSlug(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return input.ToLowerInvariant()
                        .Replace(" ", string.Empty, StringComparison.Ordinal)
                        .Replace("-", string.Empty, StringComparison.Ordinal)
                        .Replace("'", string.Empty, StringComparison.Ordinal)
                        .Replace(".", string.Empty, StringComparison.Ordinal)
                        .Replace("(", string.Empty, StringComparison.Ordinal)
                        .Replace(")", string.Empty, StringComparison.Ordinal);
        }

        private readonly record struct SpawnSeedRow(
            int X,
            int Y,
            byte Z,
            string MonsterName,
            string MonsterNameSlug,
            int? SpawnTimeSeconds,
            byte? Direction);
    }
}
