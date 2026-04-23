using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Creatures;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Http.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Services.TibiaData
{
    public sealed class CreatureSyncService(
        TibiaDataClient client,
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<CreatureSyncService> logger) : ICreatureSyncService
    {
        public async Task SyncCreaturesAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Starting Creature Sync (Boosted & Images)...");

            // 1. API Abruf
            TibiaDataCreaturesResponse? apiData = await client.GetCreaturesAsync(ct);
            if(apiData?.Creatures == null || apiData.Creatures.CreatureList.Count == 0)
            {
                logger.LogWarning("TibiaData returned no creatures.");
                return;
            }

            // 2. DB Context
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);

            // Wir laden alle Monster
            List<CreatureEntity> dbCreatures = await db.Creatures.ToListAsync(ct);

            // 3. Smart Dictionary bauen (Slug -> Entity)
            // Wir bauen einen Index, der "falconknight" auf die Entity "Falcon Knight" mappt.
            Dictionary<string, CreatureEntity> creatureMap = new();

            foreach(CreatureEntity c in dbCreatures)
            {
                // Wir nutzen ActualName (das ist der sauberste Name im Wiki)
                string slug = ToSlug(c.ActualName);
                if(!creatureMap.ContainsKey(slug))
                {
                    creatureMap[slug] = c;
                }

                // Optional: Auch den normalen Namen mappen, falls ActualName abweicht
                string nameSlug = ToSlug(c.Name);
                if(!creatureMap.ContainsKey(nameSlug))
                {
                    creatureMap[nameSlug] = c;
                }
            }

            int updatedCount = 0;
            int matchCount = 0;

            // Boosted Race Slug berechnen
            string boostedRaceSlug = ToSlug(apiData.Creatures.Boosted.Race);
            string boostedNameSlug = ToSlug(apiData.Creatures.Boosted.Name); // Fallback

            // Reset Boosted Flag für alle
            foreach(CreatureEntity c in dbCreatures)
            {
                c.IsBoosted = false;
            }

            // 4. API Liste durchgehen
            foreach(TibiaDataCreatureEntry apiEntry in apiData.Creatures.CreatureList)
            {
                // Wir versuchen zu matchen: Erst über Race, dann über Name (Plural oft tricky, aber Slug hilft)
                string apiRaceSlug = ToSlug(apiEntry.Race);
                string apiNameSlug = ToSlug(apiEntry.Name);

                CreatureEntity? entity = null;

                // Versuch 1: Race Slug (z.B. "falconknight")
                if(creatureMap.TryGetValue(apiRaceSlug, out CreatureEntity? hit1))
                {
                    entity = hit1;
                }
                // Versuch 2: Name Slug (z.B. "demons" -> "demon" klappt nicht immer, aber "rotworm" -> "rotworm" geht)
                else if(creatureMap.TryGetValue(apiNameSlug, out CreatureEntity? hit2))
                {
                    entity = hit2;
                }

                if(entity == null)
                {
                    // Logging für Debugging (kommentiere aus, wenn es zu viel wird)
                    // logger.LogTrace("Could not map API creature: {Race} / {Name}", apiEntry.Race, apiEntry.Name);
                    continue;
                }

                matchCount++;
                bool changed = false;

                // URL Update
                if(!string.IsNullOrWhiteSpace(apiEntry.ImageUrl) && entity.ImageUrl != apiEntry.ImageUrl)
                {
                    entity.ImageUrl = apiEntry.ImageUrl;
                    changed = true;
                }

                // Boosted Check
                // Wir prüfen, ob der aktuelle API-Eintrag der geboostete ist
                if(apiRaceSlug == boostedRaceSlug || apiNameSlug == boostedNameSlug)
                {
                    entity.IsBoosted = true;
                    changed = true;
                    logger.LogInformation("🔥 BOOSTED CREATURE IDENTIFIED: {Name}", entity.Name);
                }

                if(changed)
                {
                    updatedCount++;
                }
            }

            if(updatedCount > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            logger.LogInformation("Sync Stats: API Items: {Api}, DB Items: {Db}, Matched: {Match}, Updated: {Upd}",
                apiData.Creatures.CreatureList.Count,
                dbCreatures.Count,
                matchCount,
                updatedCount);
        }

        /// <summary>
        ///     Retrieves the list of boosted creatures from the database.
        /// </summary>
        /// <param name="ct">The cancellation token to cancel the operation if necessary.</param>
        /// <returns>A list of boosted creatures.</returns>
        public async Task<List<CreatureEntity>> GetBoostedCreaturesAsync(CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            return await db.Creatures.Where(c => c.IsBoosted).ToListAsync(ct);
        }

        /// <summary>
        ///     Retrieves the list of creatures that have an associated image.
        /// </summary>
        /// <param name="ct">The cancellation token to cancel the operation if necessary.</param>
        /// <returns>A list of creatures with non-null image URLs.</returns>
        public async Task<List<CreatureEntity>> GetCreaturesWithImage(CancellationToken ct = default)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync(ct);
            return await db.Creatures.Where(c => c.ImageUrl != null).ToListAsync(ct);
        }

        /// <summary>
        ///     Erstellt einen vereinfachten String für den Vergleich.
        ///     "Falcon Knight" -> "falconknight"
        ///     "Cobra Vizier" -> "cobravizier"
        /// </summary>
        private static string ToSlug(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return "";
            }
            return input.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("'", "")
                        .Replace(".", ""); // Dr. -> dr
        }
    }
}
