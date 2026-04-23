using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // --- Characters ---
        public DbSet<CharacterEntity> Characters => Set<CharacterEntity>();

        public DbSet<CharacterAccountEntity> CharacterAccounts => Set<CharacterAccountEntity>();

        public DbSet<CharacterBadgeEntity> CharacterBadges => Set<CharacterBadgeEntity>();

        public DbSet<CharacterAchievementEntity> CharacterAchievements => Set<CharacterAchievementEntity>();

        public DbSet<CharacterHouseEntity> CharacterHouses => Set<CharacterHouseEntity>();

        public DbSet<CharacterDeathEntity> CharacterDeaths => Set<CharacterDeathEntity>();

        public DbSet<CharacterSnapshotEntity> CharacterSnapshots => Set<CharacterSnapshotEntity>();

        public DbSet<CharacterGoalEntity> CharacterGoals => Set<CharacterGoalEntity>();

        public DbSet<HuntGoalConnectionEntity> HuntGoalConnections => Set<HuntGoalConnectionEntity>();

        public DbSet<CharacterDepotSaleEntity> CharacterDepotSales => Set<CharacterDepotSaleEntity>();

        // --- Items ---
        public DbSet<ItemEntity> Items => Set<ItemEntity>();

        // -- Creatures --
        public DbSet<CreatureEntity> Creatures => Set<CreatureEntity>();

        public DbSet<CreatureLootEntity> CreatureLoots => Set<CreatureLootEntity>();

        public DbSet<CreatureSoundEntity> CreatureSounds => Set<CreatureSoundEntity>();

        // -- Hunting Places --
        public DbSet<HuntingPlaceEntity> HuntingPlaces => Set<HuntingPlaceEntity>();

        public DbSet<HuntingPlaceLevelEntity> HuntingPlaceLevels => Set<HuntingPlaceLevelEntity>();

        public DbSet<HuntingPlaceCreatureEntity> HuntingPlaceCreatures => Set<HuntingPlaceCreatureEntity>();

        // -- Monster Spawn Map --
        public DbSet<MonsterSpawnCoordinateEntity> MonsterSpawnCoordinates => Set<MonsterSpawnCoordinateEntity>();

        public DbSet<MonsterSpawnCreatureLinkEntity> MonsterSpawnCreatureLinks => Set<MonsterSpawnCreatureLinkEntity>();

        // -- Monster Image Catalog --
        public DbSet<MonsterImageAssetEntity> MonsterImageAssets => Set<MonsterImageAssetEntity>();

        public DbSet<MonsterImageAliasEntity> MonsterImageAliases => Set<MonsterImageAliasEntity>();

        public DbSet<CreatureMonsterImageLinkEntity> CreatureMonsterImageLinks => Set<CreatureMonsterImageLinkEntity>();

        // -- Hunt Analyzer --
        public DbSet<HuntSessionEntity> HuntSessions => Set<HuntSessionEntity>();

        public DbSet<TeamHuntSessionEntity> TeamHuntSessions => Set<TeamHuntSessionEntity>();

        public DbSet<TeamHuntMemberEntity> TeamHuntMembers => Set<TeamHuntMemberEntity>();

        public DbSet<HuntLootEntry> HuntLootEntries => Set<HuntLootEntry>();

        public DbSet<HuntMonsterEntry> HuntMonsterEntries => Set<HuntMonsterEntry>();

        public DbSet<HuntSupplyAdjustment> HuntSupplyAdjustments => Set<HuntSupplyAdjustment>();

        public DbSet<HuntGroupEntity> HuntGroups => Set<HuntGroupEntity>();

        // -- Imbuements --
        public DbSet<ImbuementRecipeEntity> ImbuementRecipes => Set<ImbuementRecipeEntity>();

        public DbSet<ImbuementIngredientEntity> ImbuementIngredients => Set<ImbuementIngredientEntity>();

        public DbSet<UserItemPriceEntity> UserItemPrices => Set<UserItemPriceEntity>();

        public DbSet<ImbuementProfileEntity> ImbuementProfiles => Set<ImbuementProfileEntity>();

        public DbSet<CharacterActiveImbuement> CharacterActiveImbuements => Set<CharacterActiveImbuement>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
