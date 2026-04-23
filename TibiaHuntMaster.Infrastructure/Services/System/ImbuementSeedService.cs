using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Services.System
{
    public sealed class ImbuementSeedService(IDbContextFactory<AppDbContext> dbFactory, ILogger<ImbuementSeedService> logger)
    {
        public async Task EnsureRecipesSeededAsync()
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();

            // Check: Wenn schon Rezepte da sind, nichts tun.
            if(await db.ImbuementRecipes.AnyAsync())
            {
                return;
            }

            logger.LogInformation("Seeding Imbuement Recipes...");

            // HELPER: Robustes Finden der ID über den NormalizedName (UPPERCASE)
            // Das ignoriert Groß-/Kleinschreibung komplett und ist sehr schnell.
            async Task<int> GetId(string name)
            {
                string search = name.Trim().ToUpperInvariant();

                ItemEntity? item = await db.Items
                                           .AsNoTracking()
                                           // Fallback: Falls NormalizedName leer ist (alte Daten), suche über normalen Namen
                                           .FirstOrDefaultAsync(i => i.NormalizedName == search) ?? await db.Items
                                                                                                            .AsNoTracking()
                                                                                                            .FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());

                if(item == null)
                {
                    logger.LogWarning($"SEED WARNING: Item '{name}' not found in DB! Imbuement recipe might be skipped.");
                }

                return item?.Id ?? 0;
            }

            // --- 1. CORE (Mana, Life, Crit) ---

            // Mana Leech (Void)
            await CreateRecipe(db,
                "Basic Void",
                ImbuementType.Void,
                ImbuementTier.Basic,
                7500,
                await GetId("Rope Belt"),
                25);
            await CreateRecipe(db,
                "Intricate Void",
                ImbuementType.Void,
                ImbuementTier.Intricate,
                60000,
                await GetId("Rope Belt"),
                25,
                await GetId("Silencer Claws"),
                25);
            await CreateRecipe(db,
                "Powerful Void",
                ImbuementType.Void,
                ImbuementTier.Powerful,
                250000,
                await GetId("Rope Belt"),
                25,
                await GetId("Silencer Claws"),
                25,
                await GetId("Some Grimeleech Wings"),
                5);

            // Life Leech (Vampirism)
            await CreateRecipe(db,
                "Basic Vampirism",
                ImbuementType.Vampirism,
                ImbuementTier.Basic,
                7500,
                await GetId("Vampire Teeth"),
                25);
            await CreateRecipe(db,
                "Intricate Vampirism",
                ImbuementType.Vampirism,
                ImbuementTier.Intricate,
                60000,
                await GetId("Vampire Teeth"),
                25,
                await GetId("Bloody Pincers"),
                15);
            await CreateRecipe(db,
                "Powerful Vampirism",
                ImbuementType.Vampirism,
                ImbuementTier.Powerful,
                250000,
                await GetId("Vampire Teeth"),
                25,
                await GetId("Bloody Pincers"),
                15,
                await GetId("Piece of Dead Brain"),
                5);

            // Critical Hit (Strike)
            await CreateRecipe(db,
                "Basic Strike",
                ImbuementType.Strike,
                ImbuementTier.Basic,
                7500,
                await GetId("Protective Charm"),
                20);
            await CreateRecipe(db,
                "Intricate Strike",
                ImbuementType.Strike,
                ImbuementTier.Intricate,
                60000,
                await GetId("Protective Charm"),
                20,
                await GetId("Sabretooth"),
                25);
            await CreateRecipe(db,
                "Powerful Strike",
                ImbuementType.Strike,
                ImbuementTier.Powerful,
                250000,
                await GetId("Protective Charm"),
                20,
                await GetId("Sabretooth"),
                25,
                await GetId("Vexclaw Talon"),
                5);

            // --- 2. SKILLS (Magic, Distance, Melee) ---

            // Magic Level (Epiphany)
            await CreateRecipe(db,
                "Basic Epiphany",
                ImbuementType.Skill,
                ImbuementTier.Basic,
                7500,
                await GetId("Elvish Talisman"),
                25);
            await CreateRecipe(db,
                "Intricate Epiphany",
                ImbuementType.Skill,
                ImbuementTier.Intricate,
                60000,
                await GetId("Elvish Talisman"),
                25,
                await GetId("Broken Shamanic Staff"),
                15);
            await CreateRecipe(db,
                "Powerful Epiphany",
                ImbuementType.Skill,
                ImbuementTier.Powerful,
                250000,
                await GetId("Elvish Talisman"),
                25,
                await GetId("Broken Shamanic Staff"),
                15,
                await GetId("Strand of Medusa Hair"),
                15);

            // Distance (Precision)
            await CreateRecipe(db,
                "Basic Precision",
                ImbuementType.Skill,
                ImbuementTier.Basic,
                7500,
                await GetId("Elven Scouting Glass"),
                25);
            await CreateRecipe(db,
                "Intricate Precision",
                ImbuementType.Skill,
                ImbuementTier.Intricate,
                60000,
                await GetId("Elven Scouting Glass"),
                25,
                await GetId("Elven Hoof"),
                15);
            await CreateRecipe(db,
                "Powerful Precision",
                ImbuementType.Skill,
                ImbuementTier.Powerful,
                250000,
                await GetId("Elven Scouting Glass"),
                25,
                await GetId("Elven Hoof"),
                15,
                await GetId("Metal Spike"),
                10);

            // Sword (Slash)
            await CreateRecipe(db,
                "Basic Slash",
                ImbuementType.Skill,
                ImbuementTier.Basic,
                7500,
                await GetId("Lion's Mane"),
                25);
            await CreateRecipe(db,
                "Intricate Slash",
                ImbuementType.Skill,
                ImbuementTier.Intricate,
                60000,
                await GetId("Lion's Mane"),
                25,
                await GetId("Mooh'tah Shell"),
                25);
            await CreateRecipe(db,
                "Powerful Slash",
                ImbuementType.Skill,
                ImbuementTier.Powerful,
                250000,
                await GetId("Lion's Mane"),
                25,
                await GetId("Mooh'tah Shell"),
                25,
                await GetId("War Crystal"),
                5);

            // Axe (Chop)
            await CreateRecipe(db,
                "Basic Chop",
                ImbuementType.Skill,
                ImbuementTier.Basic,
                7500,
                await GetId("Orc Tooth"),
                20);
            await CreateRecipe(db,
                "Intricate Chop",
                ImbuementType.Skill,
                ImbuementTier.Intricate,
                60000,
                await GetId("Orc Tooth"),
                20,
                await GetId("Battle Stone"),
                25);
            await CreateRecipe(db,
                "Powerful Chop",
                ImbuementType.Skill,
                ImbuementTier.Powerful,
                250000,
                await GetId("Orc Tooth"),
                20,
                await GetId("Battle Stone"),
                25,
                await GetId("Moohtant Horn"),
                20);

            // Club (Bash)
            await CreateRecipe(db,
                "Basic Bash",
                ImbuementType.Skill,
                ImbuementTier.Basic,
                7500,
                await GetId("Cyclops Toe"),
                20);
            await CreateRecipe(db,
                "Intricate Bash",
                ImbuementType.Skill,
                ImbuementTier.Intricate,
                60000,
                await GetId("Cyclops Toe"),
                20,
                await GetId("Ogre Nose Ring"),
                15);
            await CreateRecipe(db,
                "Powerful Bash",
                ImbuementType.Skill,
                ImbuementTier.Powerful,
                250000,
                await GetId("Cyclops Toe"),
                20,
                await GetId("Ogre Nose Ring"),
                15,
                await GetId("Warmaster's Wristguards"),
                10);

            // --- 3. UTILITY (Speed, Cap) ---

            // Speed (Swiftness)
            await CreateRecipe(db,
                "Basic Swiftness",
                ImbuementType.Utility,
                ImbuementTier.Basic,
                7500,
                await GetId("Damselfly Wing"),
                15);
            await CreateRecipe(db,
                "Intricate Swiftness",
                ImbuementType.Utility,
                ImbuementTier.Intricate,
                60000,
                await GetId("Damselfly Wing"),
                15,
                await GetId("Compass"),
                25);
            await CreateRecipe(db,
                "Powerful Swiftness",
                ImbuementType.Utility,
                ImbuementTier.Powerful,
                250000,
                await GetId("Damselfly Wing"),
                15,
                await GetId("Compass"),
                25,
                await GetId("Waspoid Wing"),
                20);

            // Capacity (Featherweight)
            await CreateRecipe(db,
                "Basic Featherweight",
                ImbuementType.Utility,
                ImbuementTier.Basic,
                7500,
                await GetId("Fairy Wings"),
                20);
            await CreateRecipe(db,
                "Intricate Featherweight",
                ImbuementType.Utility,
                ImbuementTier.Intricate,
                60000,
                await GetId("Fairy Wings"),
                20,
                await GetId("Little Bowl of Myrrh"),
                10);
            await CreateRecipe(db,
                "Powerful Featherweight",
                ImbuementType.Utility,
                ImbuementTier.Powerful,
                250000,
                await GetId("Fairy Wings"),
                20,
                await GetId("Little Bowl of Myrrh"),
                10,
                await GetId("Goosebump Leather"),
                5);

            // --- 4. PROTECTION (Defensive) ---

            // Death Protection (Lich Shroud)
            await CreateRecipe(db,
                "Basic Lich Shroud",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Flask of Embalming Fluid"),
                25);
            await CreateRecipe(db,
                "Intricate Lich Shroud",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Flask of Embalming Fluid"),
                25,
                await GetId("Gloom Wolf Fur"),
                20);
            await CreateRecipe(db,
                "Powerful Lich Shroud",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Flask of Embalming Fluid"),
                25,
                await GetId("Gloom Wolf Fur"),
                20,
                await GetId("Mystical Hourglass"),
                5);

            // Earth Protection (Snake Skin)
            await CreateRecipe(db,
                "Basic Snake Skin",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Piece of Swampling Wood"),
                25);
            await CreateRecipe(db,
                "Intricate Snake Skin",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Piece of Swampling Wood"),
                25,
                await GetId("Snake Skin"),
                20);
            await CreateRecipe(db,
                "Powerful Snake Skin",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Piece of Swampling Wood"),
                25,
                await GetId("Snake Skin"),
                20,
                await GetId("Brimstone Fangs"),
                10);

            // Fire Protection (Dragon Hide)
            await CreateRecipe(db,
                "Basic Dragon Hide",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Green Dragon Leather"),
                20);
            await CreateRecipe(db,
                "Intricate Dragon Hide",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Green Dragon Leather"),
                20,
                await GetId("Red Dragon Leather"),
                10);
            await CreateRecipe(db,
                "Powerful Dragon Hide",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Green Dragon Leather"),
                20,
                await GetId("Red Dragon Leather"),
                10,
                await GetId("Hardened Bone"),
                5);

            // Ice Protection (Quara Scale)
            await CreateRecipe(db,
                "Basic Quara Scale",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Winter Wolf Fur"),
                25);
            await CreateRecipe(db,
                "Intricate Quara Scale",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Winter Wolf Fur"),
                25,
                await GetId("Thick Fur"),
                15);
            await CreateRecipe(db,
                "Powerful Quara Scale",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Winter Wolf Fur"),
                25,
                await GetId("Thick Fur"),
                15,
                await GetId("Northern Pike"),
                10);

            // Energy Protection (Cloud Fabric)
            await CreateRecipe(db,
                "Basic Cloud Fabric",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Wool"),
                20);
            await CreateRecipe(db,
                "Intricate Cloud Fabric",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Wool"),
                20,
                await GetId("Crawler Head Plating"),
                15);
            await CreateRecipe(db,
                "Powerful Cloud Fabric",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Wool"),
                20,
                await GetId("Crawler Head Plating"),
                15,
                await GetId("Wyrm Scale"),
                10);

            // Holy Protection (Demon Presence)
            await CreateRecipe(db,
                "Basic Demon Presence",
                ImbuementType.Protection,
                ImbuementTier.Basic,
                7500,
                await GetId("Cultish Robe"),
                25);
            await CreateRecipe(db,
                "Intricate Demon Presence",
                ImbuementType.Protection,
                ImbuementTier.Intricate,
                60000,
                await GetId("Cultish Robe"),
                25,
                await GetId("Cultish Mask"),
                25);
            await CreateRecipe(db,
                "Powerful Demon Presence",
                ImbuementType.Protection,
                ImbuementTier.Powerful,
                250000,
                await GetId("Cultish Robe"),
                25,
                await GetId("Cultish Mask"),
                25,
                await GetId("Hellspawn Tail"),
                20);

            // Save all
            await db.SaveChangesAsync();
            logger.LogInformation("Imbuement Seeding Complete.");
        }

        private async Task CreateRecipe(AppDbContext db, string name, ImbuementType type, ImbuementTier tier, long fee,
            int id1, int count1, int id2 = 0, int count2 = 0, int id3 = 0, int count3 = 0)
        {
            if(id1 == 0)
            {
                return; // Rezept kann nicht erstellt werden, wenn Haupt-Item fehlt
            }

            ImbuementRecipeEntity recipe = new()
            {
                Name = name,
                Type = type,
                Tier = tier,
                BaseFee = fee
            };

            db.ImbuementRecipes.Add(recipe);
            await db.SaveChangesAsync(); // Um ID für Ingredients zu erhalten

            db.ImbuementIngredients.Add(new ImbuementIngredientEntity
            {
                ImbuementRecipeId = recipe.Id,
                ItemId = id1,
                Amount = count1
            });

            if(id2 > 0)
            {
                db.ImbuementIngredients.Add(new ImbuementIngredientEntity
                {
                    ImbuementRecipeId = recipe.Id,
                    ItemId = id2,
                    Amount = count2
                });
            }

            if(id3 > 0)
            {
                db.ImbuementIngredients.Add(new ImbuementIngredientEntity
                {
                    ImbuementRecipeId = recipe.Id,
                    ItemId = id3,
                    Amount = count3
                });
            }
        }
    }
}
