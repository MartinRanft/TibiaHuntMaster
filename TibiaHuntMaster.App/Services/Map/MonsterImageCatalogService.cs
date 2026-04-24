using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Avalonia.Platform;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;

namespace TibiaHuntMaster.App.Services.Map
{
    public sealed class MonsterImageCatalogService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<MonsterImageCatalogService> logger) : IMonsterImageCatalogService
    {
        private static readonly Uri AssetBaseUri = new("avares://TibiaHuntMaster.App/");
        private static readonly Uri MonsterImageFolderUri = new("avares://TibiaHuntMaster.App/Assets/monster_images/");
        private static readonly Uri MonsterImageAliasManifestUri = new("avares://TibiaHuntMaster.App/Assets/monster_images/aliases.json");

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
            ["oldwolf"] = ["wolfnostalgia", "wolf"]
        };

        private readonly SemaphoreSlim _sync = new(1, 1);

        private Dictionary<int, string> _imageUriByCreatureId = new();
        private Dictionary<string, string> _imageUriBySlug = new(StringComparer.Ordinal);

        private bool _assetsEnsured;
        private int _lastCreatureCount = -1;
        private int _lastLinkCount = -1;

        public string DeathFallbackImageUri => "avares://TibiaHuntMaster.App/Assets/Standalone/DeathSplash_2x.gif";

        public string PlayerKillerImageUri => "avares://TibiaHuntMaster.App/Assets/Vocations/Monk_Artwork.png";

        public async Task EnsureCatalogAsync(CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

                if (!_assetsEnsured)
                {
                    await EnsureAssetCatalogAsync(db, ct);
                    _assetsEnsured = true;
                }

                int creatureCount = await db.Creatures.CountAsync(ct);
                int linkCount = await db.CreatureMonsterImageLinks.CountAsync(ct);

                bool shouldRelink = creatureCount != _lastCreatureCount ||
                                    linkCount < creatureCount ||
                                    _imageUriByCreatureId.Count == 0 ||
                                    _imageUriBySlug.Count == 0;

                if (shouldRelink)
                {
                    await RebuildCreatureLinksAsync(db, ct);
                    linkCount = await db.CreatureMonsterImageLinks.CountAsync(ct);
                }

                bool shouldRefreshCache = shouldRelink ||
                                          creatureCount != _lastCreatureCount ||
                                          linkCount != _lastLinkCount ||
                                          _imageUriBySlug.Count == 0;

                if (shouldRefreshCache)
                {
                    await RefreshMemoryCacheAsync(db, ct);
                }

                _lastCreatureCount = creatureCount;
                _lastLinkCount = linkCount;
            }
            finally
            {
                _sync.Release();
            }
        }

        public bool TryResolveImageUri(int? creatureId, string? monsterName, out string imageUri)
        {
            imageUri = string.Empty;

            if (creatureId.HasValue &&
                _imageUriByCreatureId.TryGetValue(creatureId.Value, out string? creatureUri))
            {
                imageUri = creatureUri;
                return true;
            }

            foreach (string candidate in EnumerateSingleNameCandidates(monsterName))
            {
                if (_imageUriBySlug.TryGetValue(candidate, out string? slugUri))
                {
                    imageUri = slugUri;
                    return true;
                }
            }

            return false;
        }

        private async Task EnsureAssetCatalogAsync(AppDbContext db, CancellationToken ct)
        {
            AssetScanResult scan = ScanMonsterImageAssets();

            bool hasAssets = await db.MonsterImageAssets.AnyAsync(ct);
            bool hasAliases = await db.MonsterImageAliases.AnyAsync(ct);
            bool shouldRebuild = !hasAssets || !hasAliases;

            if (!shouldRebuild)
            {
                shouldRebuild = await HasCatalogMismatchAsync(db, scan, ct);
            }

            if (!shouldRebuild)
            {
                return;
            }

            await RebuildAssetTablesAsync(db, scan, ct);

            int duplicateGroups = scan.FilesByContentHash.Count(kv => kv.Value.Count > 1);
            int duplicateFiles = scan.FilesByContentHash.Sum(kv => Math.Max(0, kv.Value.Count - 1));
            string action = (hasAssets || hasAliases) ? "rebuilt" : "seeded";

            logger.LogInformation(
                "Monster image catalog {Action}. Files={Files}, UniqueAssets={Unique}, Duplicates={DuplicateFiles} in {DuplicateGroups} groups.",
                action,
                scan.FileCount,
                scan.CanonicalByHash.Count,
                duplicateFiles,
                duplicateGroups);
        }

        private async Task<bool> HasCatalogMismatchAsync(AppDbContext db, AssetScanResult scan, CancellationToken ct)
        {
            int dbAssetCount = await db.MonsterImageAssets.CountAsync(ct);
            if (dbAssetCount != scan.CanonicalByHash.Count)
            {
                logger.LogInformation(
                    "Monster image catalog mismatch detected (asset count). DbAssets={DbAssets}, AssetFiles={AssetFiles}.",
                    dbAssetCount,
                    scan.CanonicalByHash.Count);
                return true;
            }

            int dbAliasCount = await db.MonsterImageAliases.CountAsync(ct);
            if (dbAliasCount != scan.HashByAliasSlug.Count)
            {
                logger.LogInformation(
                    "Monster image catalog mismatch detected (alias count). DbAliases={DbAliases}, AssetAliases={AssetAliases}.",
                    dbAliasCount,
                    scan.HashByAliasSlug.Count);
                return true;
            }

            List<string> dbHashes = await db.MonsterImageAssets
                                             .AsNoTracking()
                                             .Select(x => x.ContentHash)
                                             .ToListAsync(ct);

            HashSet<string> dbHashSet = new(dbHashes, StringComparer.Ordinal);
            HashSet<string> scanHashSet = new(scan.CanonicalByHash.Keys, StringComparer.Ordinal);

            if (dbHashSet.Count != scanHashSet.Count || !dbHashSet.SetEquals(scanHashSet))
            {
                logger.LogInformation("Monster image catalog mismatch detected (content hash set changed).");
                return true;
            }

            List<string> dbUris = await db.MonsterImageAssets
                                          .AsNoTracking()
                                          .Select(x => x.AssetUri)
                                          .ToListAsync(ct);

            HashSet<string> dbUriSet = new(dbUris, StringComparer.Ordinal);
            HashSet<string> scanUriSet = new(
                scan.CanonicalByHash.Values.Select(x => x.AssetUri),
                StringComparer.Ordinal);

            if (dbUriSet.Count != scanUriSet.Count || !dbUriSet.SetEquals(scanUriSet))
            {
                logger.LogInformation("Monster image catalog mismatch detected (asset uri set changed).");
                return true;
            }

            return false;
        }

        private AssetScanResult ScanMonsterImageAssets()
        {
            List<AssetFile> files = new();
            IEnumerable<Uri> assetUris = AssetLoader.GetAssets(MonsterImageFolderUri, AssetBaseUri);

            foreach (Uri assetUri in assetUris)
            {
                string fileName = Uri.UnescapeDataString(Path.GetFileName(assetUri.AbsolutePath));
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    continue;
                }

                string extension = Path.GetExtension(fileName);
                if (!IsSupportedImageExtension(extension))
                {
                    continue;
                }

                string rawName = Path.GetFileNameWithoutExtension(fileName);
                string primarySlug = ToSlug(rawName);
                if (string.IsNullOrWhiteSpace(primarySlug))
                {
                    continue;
                }

                (string contentHash, long fileSize) = ComputeHashAndSize(assetUri);
                files.Add(new AssetFile(
                    fileName,
                    assetUri.ToString(),
                    rawName,
                    primarySlug,
                    contentHash,
                    fileSize));
            }

            files.Sort((a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase));

            Dictionary<string, AssetFile> canonicalByHash = new(StringComparer.Ordinal);
            Dictionary<string, List<string>> filesByHash = new(StringComparer.Ordinal);

            foreach (AssetFile file in files)
            {
                if (!canonicalByHash.ContainsKey(file.ContentHash))
                {
                    canonicalByHash[file.ContentHash] = file;
                }

                if (!filesByHash.TryGetValue(file.ContentHash, out List<string>? list))
                {
                    list = new List<string>();
                    filesByHash[file.ContentHash] = list;
                }

                list.Add(file.FileName);
            }

            Dictionary<string, string> hashByAliasSlug = new(StringComparer.Ordinal);
            foreach (AssetFile file in files)
            {
                foreach (string alias in BuildAliasSlugs(file))
                {
                    if (!hashByAliasSlug.ContainsKey(alias))
                    {
                        hashByAliasSlug[alias] = file.ContentHash;
                    }
                }
            }

            Dictionary<string, string> pinnedAliasMap = LoadPinnedAliasMap();
            Dictionary<string, string> canonicalHashByFileName = canonicalByHash.Values.ToDictionary(
                x => x.FileName,
                x => x.ContentHash,
                StringComparer.OrdinalIgnoreCase);

            foreach ((string aliasSlug, string canonicalFileName) in pinnedAliasMap)
            {
                if (hashByAliasSlug.ContainsKey(aliasSlug))
                {
                    continue;
                }

                if (!canonicalHashByFileName.TryGetValue(canonicalFileName, out string? canonicalHash))
                {
                    continue;
                }

                hashByAliasSlug[aliasSlug] = canonicalHash;
            }

            return new AssetScanResult(files.Count, canonicalByHash, hashByAliasSlug, filesByHash);
        }

        private Dictionary<string, string> LoadPinnedAliasMap()
        {
            Dictionary<string, string> result = new(StringComparer.Ordinal);

            try
            {
                using Stream stream = AssetLoader.Open(MonsterImageAliasManifestUri, AssetBaseUri);
                using JsonDocument document = JsonDocument.Parse(stream);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return result;
                }

                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || property.Value.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    string? canonicalFileName = property.Value.GetString();
                    if (string.IsNullOrWhiteSpace(canonicalFileName))
                    {
                        continue;
                    }

                    result[property.Name] = canonicalFileName;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Monster image alias manifest could not be loaded; continuing without pinned aliases.");
            }

            return result;
        }

        private async Task RebuildAssetTablesAsync(AppDbContext db, AssetScanResult scan, CancellationToken ct)
        {
            List<MonsterImageAliasEntity> existingAliases = await db.MonsterImageAliases.ToListAsync(ct);
            if (existingAliases.Count > 0)
            {
                db.MonsterImageAliases.RemoveRange(existingAliases);
            }

            List<MonsterImageAssetEntity> existingAssets = await db.MonsterImageAssets.ToListAsync(ct);
            if (existingAssets.Count > 0)
            {
                db.MonsterImageAssets.RemoveRange(existingAssets);
            }

            if (existingAliases.Count > 0 || existingAssets.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            List<MonsterImageAssetEntity> assetsToInsert = scan.CanonicalByHash.Values
                                                         .Select(file => new MonsterImageAssetEntity
                                                         {
                                                             CanonicalSlug = file.PrimarySlug,
                                                             AssetUri = file.AssetUri,
                                                             ContentHash = file.ContentHash,
                                                             FileSizeBytes = file.FileSizeBytes
                                                         })
                                                         .ToList();

            db.MonsterImageAssets.AddRange(assetsToInsert);
            await db.SaveChangesAsync(ct);

            Dictionary<string, int> assetIdByHash = assetsToInsert.ToDictionary(
                x => x.ContentHash,
                x => x.Id,
                StringComparer.Ordinal);

            List<MonsterImageAliasEntity> aliasesToInsert = new(scan.HashByAliasSlug.Count);
            foreach ((string aliasSlug, string hash) in scan.HashByAliasSlug)
            {
                if (!assetIdByHash.TryGetValue(hash, out int assetId))
                {
                    continue;
                }

                aliasesToInsert.Add(new MonsterImageAliasEntity
                {
                    Slug = aliasSlug,
                    MonsterImageAssetId = assetId
                });
            }

            if (aliasesToInsert.Count > 0)
            {
                db.MonsterImageAliases.AddRange(aliasesToInsert);
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task RebuildCreatureLinksAsync(AppDbContext db, CancellationToken ct)
        {
            Dictionary<string, int> assetIdBySlug = await db.MonsterImageAliases
                                                            .AsNoTracking()
                                                            .ToDictionaryAsync(x => x.Slug, x => x.MonsterImageAssetId, StringComparer.Ordinal, ct);

            if (assetIdBySlug.Count == 0)
            {
                return;
            }

            List<CreatureProjection> creatures = await db.Creatures
                                                     .AsNoTracking()
                                                     .Select(x => new CreatureProjection(x.Id, x.Name, x.ActualName))
                                                     .ToListAsync(ct);

            List<CreatureMonsterImageLinkEntity> existingLinks = await db.CreatureMonsterImageLinks.ToListAsync(ct);
            Dictionary<int, CreatureMonsterImageLinkEntity> linksByCreatureId = existingLinks.ToDictionary(x => x.CreatureId);

            bool changed = false;
            foreach (CreatureProjection creature in creatures)
            {
                bool hasAsset = TryResolveAssetIdForCreature(creature, assetIdBySlug, out int assetId, out string matchedSlug);

                if (hasAsset)
                {
                    if (linksByCreatureId.TryGetValue(creature.Id, out CreatureMonsterImageLinkEntity? existing))
                    {
                        if (existing.MonsterImageAssetId != assetId || !string.Equals(existing.MatchedBySlug, matchedSlug, StringComparison.Ordinal))
                        {
                            existing.MonsterImageAssetId = assetId;
                            existing.MatchedBySlug = matchedSlug;
                            changed = true;
                        }

                        continue;
                    }

                    db.CreatureMonsterImageLinks.Add(new CreatureMonsterImageLinkEntity
                    {
                        CreatureId = creature.Id,
                        MonsterImageAssetId = assetId,
                        MatchedBySlug = matchedSlug
                    });
                    changed = true;
                    continue;
                }

                if (linksByCreatureId.TryGetValue(creature.Id, out CreatureMonsterImageLinkEntity? stale))
                {
                    db.CreatureMonsterImageLinks.Remove(stale);
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync(ct);
            }
        }

        private async Task RefreshMemoryCacheAsync(AppDbContext db, CancellationToken ct)
        {
            List<(int CreatureId, string AssetUri)> creatureRows = await db.CreatureMonsterImageLinks
                                                                            .AsNoTracking()
                                                                            .Select(x => new ValueTuple<int, string>(
                                                                                x.CreatureId,
                                                                                x.MonsterImageAsset.AssetUri))
                                                                            .ToListAsync(ct);

            List<(string Slug, string AssetUri)> slugRows = await db.MonsterImageAliases
                                                                    .AsNoTracking()
                                                                    .Select(x => new ValueTuple<string, string>(
                                                                        x.Slug,
                                                                        x.MonsterImageAsset.AssetUri))
                                                                    .ToListAsync(ct);

            Dictionary<int, string> imageByCreatureId = new();
            foreach ((int creatureId, string assetUri) in creatureRows)
            {
                if (!imageByCreatureId.ContainsKey(creatureId))
                {
                    imageByCreatureId[creatureId] = assetUri;
                }
            }

            Dictionary<string, string> imageBySlug = new(StringComparer.Ordinal);
            foreach ((string slug, string assetUri) in slugRows)
            {
                if (!imageBySlug.ContainsKey(slug))
                {
                    imageBySlug[slug] = assetUri;
                }
            }

            _imageUriByCreatureId = imageByCreatureId;
            _imageUriBySlug = imageBySlug;
        }

        private static bool TryResolveAssetIdForCreature(
            CreatureProjection creature,
            IReadOnlyDictionary<string, int> assetIdBySlug,
            out int assetId,
            out string matchedSlug)
        {
            foreach (string candidate in EnumerateCreatureCandidates(creature.Name, creature.ActualName))
            {
                if (assetIdBySlug.TryGetValue(candidate, out int resolvedAssetId))
                {
                    assetId = resolvedAssetId;
                    matchedSlug = candidate;
                    return true;
                }
            }

            assetId = 0;
            matchedSlug = string.Empty;
            return false;
        }

        private static IEnumerable<string> BuildAliasSlugs(AssetFile file)
        {
            HashSet<string> yielded = new(StringComparer.Ordinal);

            bool Add(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }

                return yielded.Add(value);
            }

            if (Add(file.PrimarySlug))
            {
                yield return file.PrimarySlug;
            }

            string withoutParentheses = Regex.Replace(file.RawName, @"\([^)]*\)", string.Empty).Trim();
            string simplifiedSlug = ToSlug(withoutParentheses);
            if (Add(simplifiedSlug))
            {
                yield return simplifiedSlug;
            }

            if (file.PrimarySlug.EndsWith("creature", StringComparison.Ordinal) &&
                file.PrimarySlug.Length > "creature".Length)
            {
                string withoutCreature = file.PrimarySlug[..^"creature".Length];
                if (Add(withoutCreature))
                {
                    yield return withoutCreature;
                }
            }

            if (file.PrimarySlug.EndsWith("nostalgia", StringComparison.Ordinal) &&
                file.PrimarySlug.Length > "nostalgia".Length)
            {
                string withoutNostalgia = file.PrimarySlug[..^"nostalgia".Length];
                if (Add(withoutNostalgia))
                {
                    yield return withoutNostalgia;
                }
            }
        }

        private static IEnumerable<string> EnumerateSingleNameCandidates(string? name)
        {
            string slug = ToSlug(name);
            if (string.IsNullOrWhiteSpace(slug))
            {
                yield break;
            }

            HashSet<string> yielded = new(StringComparer.Ordinal);
            if (yielded.Add(slug))
            {
                yield return slug;
            }

            if (yielded.Add($"{slug}creature"))
            {
                yield return $"{slug}creature";
            }

            if (yielded.Add($"{slug}nostalgia"))
            {
                yield return $"{slug}nostalgia";
            }

            if (slug.StartsWith("old", StringComparison.Ordinal) && slug.Length > 3)
            {
                string withoutOld = slug[3..];
                if (yielded.Add(withoutOld))
                {
                    yield return withoutOld;
                }

                string oldNostalgia = $"{withoutOld}nostalgia";
                if (yielded.Add(oldNostalgia))
                {
                    yield return oldNostalgia;
                }
            }

            if (NameAliasesBySlug.TryGetValue(slug, out string[]? aliases))
            {
                foreach (string alias in aliases)
                {
                    if (yielded.Add(alias))
                    {
                        yield return alias;
                    }
                }
            }
        }

        private static IEnumerable<string> EnumerateCreatureCandidates(string name, string actualName)
        {
            HashSet<string> yielded = new(StringComparer.Ordinal);

            foreach (string candidate in EnumerateSingleNameCandidates(actualName))
            {
                if (yielded.Add(candidate))
                {
                    yield return candidate;
                }
            }

            foreach (string candidate in EnumerateSingleNameCandidates(name))
            {
                if (yielded.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static bool IsSupportedImageExtension(string extension)
        {
            return extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }

        private static (string Hash, long SizeBytes) ComputeHashAndSize(Uri assetUri)
        {
            using Stream stream = AssetLoader.Open(assetUri, AssetBaseUri);
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            byte[] buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                total += read;
                hash.AppendData(buffer, 0, read);
            }

            byte[] hashBytes = hash.GetHashAndReset();
            string hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return (hashHex, total);
        }

        private static string ToSlug(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            StringBuilder builder = new(input.Length);
            foreach (char c in input.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private readonly record struct CreatureProjection(int Id, string Name, string ActualName);

        private readonly record struct AssetFile(
            string FileName,
            string AssetUri,
            string RawName,
            string PrimarySlug,
            string ContentHash,
            long FileSizeBytes);

        private readonly record struct AssetScanResult(
            int FileCount,
            IReadOnlyDictionary<string, AssetFile> CanonicalByHash,
            IReadOnlyDictionary<string, string> HashByAliasSlug,
            IReadOnlyDictionary<string, List<string>> FilesByContentHash);
    }
}
