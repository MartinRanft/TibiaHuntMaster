using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public sealed class ImbuementCalculatorService(IDbContextFactory<AppDbContext> dbFactory) : IImbuementCalculatorService
    {
        public async Task<long> CalculateHourlyCostAsync(int characterId)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            // 1. Profil laden
            ImbuementProfileEntity? profile = await db.ImbuementProfiles
                                                      .Include(p => p.ActiveImbuements)
                                                      .ThenInclude(ai => ai.Recipe)
                                                      .ThenInclude(r => r.Ingredients)
                                                      .FirstOrDefaultAsync(p => p.CharacterId == characterId);

            if(profile == null || profile.ActiveImbuements.Count == 0)
            {
                return 0;
            }

            long totalCostFor20Hours = 0;

            // 2. Preise laden (Cache könnte hier sinnvoll sein)
            Dictionary<int, long> userPrices = await db.UserItemPrices.ToDictionaryAsync(x => x.ItemId, x => x.Price);

            // 3. Berechnung
            foreach(CharacterActiveImbuement active in profile.ActiveImbuements)
            {
                long recipeCost = active.Recipe.BaseFee;

                // Blank Scroll Fee (25k) pro Imbuement? Oder pro Item? 
                // Wir nehmen an pro Imbuement-Slot.
                if(profile.UseBlankScrolls)
                {
                    recipeCost += 25_000;
                }

                // Materialkosten
                foreach(ImbuementIngredientEntity ing in active.Recipe.Ingredients)
                {
                    long price = 0;
                    if(userPrices.TryGetValue(ing.ItemId, out long userPrice))
                    {
                        price = userPrice;
                    }

                    // Fallback: Wenn User keinen Preis gesetzt hat -> 0 oder Standard-Wiki-Value?
                    // Wir nehmen 0 und warnen später in der UI.
                    recipeCost += price * ing.Amount;
                }

                // Gesamtkosten für dieses Imbuement (für 20h) * Anzahl (z.B. 2x Void)
                totalCostFor20Hours += recipeCost * active.Count;
            }

            // 4. Kosten pro Stunde
            return (long)(totalCostFor20Hours / 20.0);
        }

        // Methoden zum Speichern von Preisen und Profilen folgen...
        public async Task UpdateItemPriceAsync(int itemId, long price)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            UserItemPriceEntity? entry = await db.UserItemPrices.FirstOrDefaultAsync(x => x.ItemId == itemId);
            if(entry == null)
            {
                db.UserItemPrices.Add(new UserItemPriceEntity
                {
                    ItemId = itemId,
                    Price = price
                });
            }
            else
            {
                entry.Price = price;
                entry.LastUpdated = DateTimeOffset.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        public async Task<ImbuementProfileEntity> GetProfileAsync(int characterId)
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            // Wir laden das Profil. Wenn keins existiert, geben wir ein neues (leeres) zurück.
            // Das ViewModel kümmert sich dann um das Erstellen in der DB beim Speichern.
            ImbuementProfileEntity? profile = await db.ImbuementProfiles
                                                      .AsNoTracking()
                                                      .FirstOrDefaultAsync(p => p.CharacterId == characterId);

            if(profile == null)
            {
                return new ImbuementProfileEntity
                {
                    CharacterId = characterId,
                    UseBlankScrolls = false,
                    LastUpdated = DateTimeOffset.UtcNow
                };
            }

            return profile;
        }
    }
}