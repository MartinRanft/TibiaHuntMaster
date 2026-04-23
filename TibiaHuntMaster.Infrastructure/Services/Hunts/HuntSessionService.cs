using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Hunts;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Parsing;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    public enum SessionImportResult
    {
        Success,
        CharacterNotFound,
        ParseError,
        Duplicate
    }

    public sealed class HuntSessionService(
        IDbContextFactory<AppDbContext> dbFactory,
        HuntAnalyzerParser parser,
        ILogger<HuntSessionService> logger) : IHuntSessionService
    {
        private sealed record CharacterLookup(int Id, string Name);
        private sealed record PlaceCreatureLink(int HuntingPlaceId, int CreatureId);
        private sealed record PlaceScore(int HuntingPlaceId, int MatchedKillCount, int DistinctCreatureCount);

        /// <summary>
        ///     Imports a hunt session into the database based on the provided options.
        ///     Parses the raw data, validates it, checks for duplicates, and saves the session if valid.
        /// </summary>
        /// <param name="options">
        ///     The options containing details about the hunt session to be imported,
        ///     such as raw data, character name, and session-specific flags like
        ///     double experience, double loot, or rapid respawn.
        /// </param>
        /// <param name="ct">
        ///     The cancellation token to observe while waiting for the operation to complete.
        /// </param>
        /// <returns>
        ///     A tuple containing the result of the session import, the imported session entity (if successfully saved),
        ///     and an error message (if any issue occurs).
        /// </returns>
        public async Task<(SessionImportResult Result, HuntSessionEntity? Session, string? Error)> ImportSessionAsync(SessionImportOptions options, CancellationToken ct = default)
        {
            if (UserInputSanitizer.ExceedsLength(options.RawText, UserInputLimits.HuntLogMaxLength))
            {
                return (SessionImportResult.ParseError, null, $"Input too large (max {UserInputLimits.HuntLogMaxLength} characters).");
            }

            string safeRawText = UserInputSanitizer.Truncate(options.RawText, UserInputLimits.HuntLogMaxLength);
            return await SqliteWriteRetry.ExecuteAsync<(SessionImportResult Result, HuntSessionEntity? Session, string? Error)>(
                async token =>
                {
                    await using AppDbContext db = await dbFactory.CreateDbContextAsync(token);

                    CharacterLookup? character = await ResolveCharacterByNameAsync(db, options.CharacterName, token);
                    if (character is null)
                    {
                        logger.LogWarning("Character {Name} not found.", options.CharacterName);
                        return (SessionImportResult.CharacterNotFound, null, "Character not found in DB.");
                    }

                    if (!parser.TryParse(safeRawText, character.Id, out HuntSessionEntity? session, out string parseError))
                    {
                        logger.LogWarning("Parsing failed: {Error}", parseError);
                        return (SessionImportResult.ParseError, null, parseError);
                    }

                    session!.IsDoubleXp = options.IsDoubleXp;
                    session.IsDoubleLoot = options.IsDoubleLoot;
                    session.IsRapidRespawn = options.IsRapidRespawn;
                    session.XpBoostPercent = NormalizeOptionalPercent(options.XpBoostPercent);
                    session.XpBoostActiveMinutes = NormalizeOptionalMinutes(options.XpBoostActiveMinutes);
                    session.CustomXpRatePercent = NormalizeOptionalPercent(options.CustomXpRatePercent);
                    session.Notes = UserInputSanitizer.TrimAndTruncateOrNull(options.Notes, UserInputLimits.SessionNotesMaxLength);
                    NormalizeSessionForPersistence(session);
                    session.HuntingPlaceId = await InferHuntingPlaceIdAsync(db, session, token);

                    await using var transaction = await db.Database.BeginTransactionAsync(token);

                    bool exists = await db.HuntSessions.AnyAsync(s =>
                        s.CharacterId == character.Id &&
                        s.SessionStartTime == session.SessionStartTime &&
                        s.XpGain == session.XpGain &&
                        s.Balance == session.Balance &&
                        s.Duration == session.Duration,
                        token);

                    if (exists)
                    {
                        logger.LogInformation(
                            "Duplicate hunt session detected for character {CharacterName} (ID: {CharacterId}). XP: {XpGain}, Balance: {Balance}, Duration: {Duration}",
                            character.Name,
                            character.Id,
                            session.XpGain,
                            session.Balance,
                            session.Duration);
                        return (SessionImportResult.Duplicate, null, "Session already exists.");
                    }

                    db.HuntSessions.Add(session);
                    await db.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);

                    logger.LogInformation(
                        "Successfully imported hunt session for character {CharacterName} (ID: {CharacterId}, SessionID: {SessionId}). XP: {XpGain}, Balance: {Balance}, Duration: {Duration}",
                        character.Name,
                        character.Id,
                        session.Id,
                        session.XpGain,
                        session.Balance,
                        session.Duration);

                    return (SessionImportResult.Success, session, null);
                },
                logger,
                "solo hunt import",
                ct);
        }

        /// <summary>
        ///     Retrieves hunt statistics for a specified character based on their hunt sessions.
        ///     This includes the total session count, average experience per hour, total profit,
        ///     and average session duration.
        /// </summary>
        /// <param name="charName">
        ///     The name of the character whose hunt statistics are to be retrieved.
        /// </param>
        /// <param name="limit">
        ///     The maximum number of recent hunt sessions to include in the statistics.
        ///     Defaults to 10 if not specified.
        /// </param>
        /// <param name="ct">
        ///     The cancellation token to monitor for operation cancellation.
        /// </param>
        /// <returns>
        ///     An instance of <c>HuntStatistics</c> containing aggregated data about the specified character's hunt sessions,
        ///     or <c>null</c> if no hunt sessions are found.
        /// </returns>
        public async Task<HuntStatistics?> GetStatisticsAsync(string charName, int limit = 10, CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            CharacterLookup? character = await ResolveCharacterByNameAsync(db, charName, ct);
            if(character is null)
            {
                return null;
            }

            List<HuntSessionEntity> sessions = await db.HuntSessions
                                                       .Where(s => s.CharacterId == character.Id)
                                                       .OrderByDescending(s => s.ImportedAt)
                                                       .Take(limit)
                                                       .ToListAsync(ct);

            if(sessions.Count == 0)
            {
                return null;
            }

            return new HuntStatistics(
                sessions.Count,
                (long)sessions.Average(s => s.XpPerHour),
                sessions.Sum(s => s.Balance),
                TimeSpan.FromSeconds(sessions.Average(s => s.Duration.TotalSeconds))
            );
        }

        /// <summary>
        ///     Adds a new hunt supply adjustment record to the database.
        ///     Persists the provided adjustment entity into the database, making it available for future queries and analysis.
        /// </summary>
        /// <param name="adj">
        ///     The hunt supply adjustment entity to be added. Contains details such as
        ///     the associated hunt session, item name, value, and adjustment type.
        /// </param>
        /// <returns>
        ///     A task representing the asynchronous operation of saving the hunt supply adjustment.
        /// </returns>
        public async Task AddAdjustmentAsync(HuntSupplyAdjustment adj)
        {
            NormalizeAdjustment(adj);
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.HuntSupplyAdjustments.Add(adj);
            await db.SaveChangesAsync();
        }

        /// <summary>
        ///     Removes a specific supply adjustment entry from the database if it exists.
        /// </summary>
        /// <param name="adj">
        ///     The adjustment to be removed. It must include the adjustment's unique identifier (Id).
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous removal operation. If the adjustment exists and is successfully removed,
        ///     the database state will be updated accordingly. Otherwise, no action will be taken.
        /// </returns>
        public async Task RemoveAdjustmentAsync(HuntSupplyAdjustment adj)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            HuntSupplyAdjustment? entry = await db.HuntSupplyAdjustments.FindAsync(adj.Id);
            if(entry != null)
            {
                db.HuntSupplyAdjustments.Remove(entry);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        ///     Updates an existing hunt supply adjustment entry in the database.
        /// </summary>
        /// <param name="adj">
        ///     The hunt supply adjustment entity containing the updated information,
        ///     such as the adjustment name, value, type, and associated hunt session ID.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous update operation.
        ///     Completes once the database changes have been saved.
        /// </returns>
        public async Task UpdateAdjustmentAsync(HuntSupplyAdjustment adj)
        {
            NormalizeAdjustment(adj);
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.HuntSupplyAdjustments.Update(adj);
            await db.SaveChangesAsync();
        }

        /// <summary>
        ///     Replaces the existing supply adjustments for a specific hunt session with a new list of adjustments.
        ///     First, all existing adjustments tied to the specified session are removed from the database,
        ///     and then the new adjustments are added and saved.
        /// </summary>
        /// <param name="session">
        ///     The identifier of the hunt session for which the supply adjustments are to be replaced.
        /// </param>
        /// <param name="newAdjustments">
        ///     A list of new supply adjustments to be associated with the specified hunt session.
        ///     Each adjustment will be added to the database with its ID reset and tied to the given session ID.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        /// </returns>
        public async Task ReplacedAdjustmentAsync(int session, List<HuntSupplyAdjustment> newAdjustments)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            List<HuntSupplyAdjustment> oldAdjustments = await db.HuntSupplyAdjustments.Where(a => a.HuntSessionId == session).ToListAsync();
            db.HuntSupplyAdjustments.RemoveRange(oldAdjustments);

            foreach(HuntSupplyAdjustment adj in newAdjustments)
            {
                adj.Id = 0;
                adj.HuntSessionId = session;
                NormalizeAdjustment(adj);
                db.HuntSupplyAdjustments.Add(adj);
            }
            await db.SaveChangesAsync();
        }
        public async Task UpdateSessionAsync(HuntSessionEntity currentSession)
        {
            NormalizeSessionForPersistence(currentSession);
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            db.HuntSessions.Update(currentSession);
            int test = await db.SaveChangesAsync();
            logger.LogInformation($"changed {test} entries");
        }

        public async Task<int> BackfillMissingHuntingPlaceLinksAsync(int batchSize = 300, CancellationToken ct = default)
        {
            if(batchSize <= 0)
            {
                batchSize = 300;
            }

            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            int updatedCount = 0;
            int lastProcessedId = 0;

            while(true)
            {
                List<HuntSessionEntity> sessions = await db.HuntSessions
                                                          .Include(s => s.KilledMonsters)
                                                          .Where(s => s.HuntingPlaceId == null && s.Id > lastProcessedId)
                                                          .OrderBy(s => s.Id)
                                                          .Take(batchSize)
                                                          .ToListAsync(ct);
                if(sessions.Count == 0)
                {
                    break;
                }

                lastProcessedId = sessions[^1].Id;
                bool hasAnyChanges = false;

                foreach(HuntSessionEntity session in sessions)
                {
                    int? inferred = await InferHuntingPlaceIdAsync(db, session, ct);
                    if(!inferred.HasValue)
                    {
                        continue;
                    }

                    session.HuntingPlaceId = inferred.Value;
                    hasAnyChanges = true;
                    updatedCount++;
                }

                if(hasAnyChanges)
                {
                    await db.SaveChangesAsync(ct);
                }
            }

            logger.LogInformation("Backfill for HuntSession.HuntingPlaceId completed. Updated sessions: {UpdatedCount}", updatedCount);
            return updatedCount;
        }

        private static async Task<CharacterLookup?> ResolveCharacterByNameAsync(AppDbContext db, string characterName, CancellationToken ct)
        {
            if(string.IsNullOrWhiteSpace(characterName))
            {
                return null;
            }

            string requestedName = UserInputSanitizer.TrimAndTruncate(characterName, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(requestedName))
            {
                return null;
            }

            CharacterLookup? exactMatch = await db.Characters
                                                  .AsNoTracking()
                                                  .Where(c => c.Name == requestedName)
                                                  .Select(c => new CharacterLookup(c.Id, c.Name))
                                                  .FirstOrDefaultAsync(ct);
            if(exactMatch is not null)
            {
                return exactMatch;
            }

            // Provider-safe fallback without culture-sensitive SQL lower-casing.
            List<CharacterLookup> allCharacters = await db.Characters
                                                          .AsNoTracking()
                                                          .Select(c => new CharacterLookup(c.Id, c.Name))
                                                          .ToListAsync(ct);

            return allCharacters.FirstOrDefault(c =>
                string.Equals(c.Name, requestedName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<int?> InferHuntingPlaceIdAsync(AppDbContext db, HuntSessionEntity session, CancellationToken ct)
        {
            if(session.KilledMonsters.Count == 0)
            {
                return null;
            }

            Dictionary<string, int> killsByName = session.KilledMonsters
                                                         .Where(m => m.Amount > 0)
                                                         .Select(m => new
                                                         {
                                                             Name = NormalizeMonsterName(m.MonsterName),
                                                             m.Amount
                                                         })
                                                         .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                                                         .GroupBy(x => x.Name, StringComparer.Ordinal)
                                                         .ToDictionary(
                                                             g => g.Key,
                                                             g => g.Sum(x => x.Amount),
                                                             StringComparer.Ordinal);
            if(killsByName.Count == 0)
            {
                return null;
            }

            Dictionary<string, HashSet<int>> creatureIdsByName = new(StringComparer.Ordinal);
            var creatures = await db.Creatures
                                    .AsNoTracking()
                                    .Select(c => new
                                    {
                                        c.Id,
                                        c.Name,
                                        c.ActualName
                                    })
                                    .ToListAsync(ct);

            foreach(var creature in creatures)
            {
                AddCreatureNameLookup(creatureIdsByName, NormalizeMonsterName(creature.Name), creature.Id);
                AddCreatureNameLookup(creatureIdsByName, NormalizeMonsterName(creature.ActualName), creature.Id);
            }

            Dictionary<int, int> killsByCreatureId = new();
            foreach(var entry in killsByName)
            {
                if(!creatureIdsByName.TryGetValue(entry.Key, out HashSet<int>? creatureIds))
                {
                    continue;
                }

                foreach(int creatureId in creatureIds)
                {
                    killsByCreatureId.TryGetValue(creatureId, out int currentKills);
                    killsByCreatureId[creatureId] = currentKills + entry.Value;
                }
            }

            if(killsByCreatureId.Count == 0)
            {
                return null;
            }

            int[] matchedCreatureIds = killsByCreatureId.Keys.ToArray();
            var placeLinks = await db.HuntingPlaceCreatures
                                     .AsNoTracking()
                                     .Where(link => link.CreatureId.HasValue && matchedCreatureIds.Contains(link.CreatureId.Value))
                                     .Select(link => new PlaceCreatureLink(link.HuntingPlaceId, link.CreatureId!.Value))
                                     .ToListAsync(ct);
            if(placeLinks.Count == 0)
            {
                return null;
            }

            int matchedKillsTotal = killsByCreatureId.Values.Sum();
            if(matchedKillsTotal <= 0)
            {
                return null;
            }

            List<PlaceScore> placeScores = placeLinks
                                           .GroupBy(link => link.HuntingPlaceId)
                                           .Select(group =>
                                           {
                                               int matchedKills = 0;
                                               HashSet<int> distinctCreatures = new();

                                               foreach(PlaceCreatureLink link in group)
                                               {
                                                   if(!killsByCreatureId.TryGetValue(link.CreatureId, out int kills))
                                                   {
                                                       continue;
                                                   }

                                                   matchedKills += kills;
                                                   distinctCreatures.Add(link.CreatureId);
                                               }

                                               return new PlaceScore(group.Key, matchedKills, distinctCreatures.Count);
                                           })
                                           .Where(score => score.MatchedKillCount > 0)
                                           .OrderByDescending(score => score.MatchedKillCount)
                                           .ThenByDescending(score => score.DistinctCreatureCount)
                                           .ToList();

            if(placeScores.Count == 0)
            {
                return null;
            }

            PlaceScore best = placeScores[0];
            double bestCoverage = (double)best.MatchedKillCount / matchedKillsTotal;
            if(bestCoverage < 0.35d)
            {
                return null;
            }

            if(best.DistinctCreatureCount <= 1 && bestCoverage < 0.60d)
            {
                return null;
            }

            if(placeScores.Count > 1)
            {
                PlaceScore runnerUp = placeScores[1];
                double runnerUpCoverage = (double)runnerUp.MatchedKillCount / matchedKillsTotal;
                bool tooClose = Math.Abs(bestCoverage - runnerUpCoverage) < 0.10d &&
                                Math.Abs(best.DistinctCreatureCount - runnerUp.DistinctCreatureCount) <= 1;
                if(tooClose)
                {
                    return null;
                }
            }

            return best.HuntingPlaceId;
        }

        private static void AddCreatureNameLookup(Dictionary<string, HashSet<int>> lookup, string normalizedName, int creatureId)
        {
            if(string.IsNullOrWhiteSpace(normalizedName))
            {
                return;
            }

            if(!lookup.TryGetValue(normalizedName, out HashSet<int>? creatureIds))
            {
                creatureIds = new HashSet<int>();
                lookup[normalizedName] = creatureIds;
            }

            creatureIds.Add(creatureId);
        }

        private static string NormalizeMonsterName(string? rawName)
        {
            if(string.IsNullOrWhiteSpace(rawName))
            {
                return string.Empty;
            }

            string normalized = rawName.Trim()
                                       .Replace('’', '\'')
                                       .Replace('-', ' ')
                                       .ToLowerInvariant();

            if(normalized.StartsWith("a ", StringComparison.Ordinal))
            {
                normalized = normalized[2..];
            }
            else if(normalized.StartsWith("an ", StringComparison.Ordinal))
            {
                normalized = normalized[3..];
            }
            else if(normalized.StartsWith("the ", StringComparison.Ordinal))
            {
                normalized = normalized[4..];
            }

            string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts);
        }

        private static void NormalizeSessionForPersistence(HuntSessionEntity session)
        {
            session.RawInput = UserInputSanitizer.Truncate(session.RawInput, UserInputLimits.HuntLogMaxLength);
            session.Notes = UserInputSanitizer.TrimAndTruncateOrNull(session.Notes, UserInputLimits.SessionNotesMaxLength);
            session.XpBoostPercent = NormalizeOptionalPercent(session.XpBoostPercent);
            session.XpBoostActiveMinutes = NormalizeOptionalMinutes(session.XpBoostActiveMinutes);
            session.CustomXpRatePercent = NormalizeOptionalPercent(session.CustomXpRatePercent);

            for(int i = session.KilledMonsters.Count - 1; i >= 0; i--)
            {
                HuntMonsterEntry entry = session.KilledMonsters[i];
                entry.MonsterName = UserInputSanitizer.TrimAndTruncate(entry.MonsterName, UserInputLimits.HuntEntryNameMaxLength);
                if(entry.Amount <= 0 || string.IsNullOrWhiteSpace(entry.MonsterName))
                {
                    session.KilledMonsters.RemoveAt(i);
                }
            }

            for(int i = session.LootItems.Count - 1; i >= 0; i--)
            {
                HuntLootEntry entry = session.LootItems[i];
                entry.ItemName = UserInputSanitizer.TrimAndTruncate(entry.ItemName, UserInputLimits.HuntEntryNameMaxLength);
                if(entry.Amount <= 0 || string.IsNullOrWhiteSpace(entry.ItemName))
                {
                    session.LootItems.RemoveAt(i);
                }
            }
        }

        private static void NormalizeAdjustment(HuntSupplyAdjustment adj)
        {
            string safeName = UserInputSanitizer.TrimAndTruncate(adj.Name, UserInputLimits.HuntAdjustmentNameMaxLength);
            adj.Name = string.IsNullOrWhiteSpace(safeName) ? "Adjustment" : safeName;
        }

        private static int? NormalizeOptionalPercent(int? value)
        {
            if(!value.HasValue || value.Value <= 0)
            {
                return null;
            }

            return Math.Min(value.Value, 10_000);
        }

        private static int? NormalizeOptionalMinutes(int? value)
        {
            if(!value.HasValue || value.Value <= 0)
            {
                return null;
            }

            return Math.Min(value.Value, 10_000);
        }
    }

    public sealed record HuntStatistics(
        int SessionCount,
        long AvgXpPerHour,
        long TotalProfit,
        TimeSpan AvgDuration
    );
}
