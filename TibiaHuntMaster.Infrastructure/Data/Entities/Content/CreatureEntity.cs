using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Content
{
    [Index(nameof(ContentId), IsUnique = true)]
    [Table("Creatures")]
    public sealed class CreatureEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ContentId { get; set; }

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string ActualName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Plural { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Article { get; set; }

        [MaxLength(512)]
        public string? ImageUrl { get; set; }

        [MaxLength(64)]
        public string? TemplateType { get; set; }

        [MaxLength(128)]
        public string? PrimaryType { get; set; }

        [MaxLength(128)]
        public string? SecondaryType { get; set; }

        [MaxLength(128)]
        public string? CreatureClass { get; set; }

        public bool? IsBoss { get; set; }

        public bool IsBoosted { get; set; }

        public int? Hp { get; set; }

        public long? Exp { get; set; }

        public int? Armor { get; set; }

        public double? Mitigation { get; set; }

        public int? MaxDmg { get; set; }

        [MaxLength(512)]
        public string? Abilities { get; set; }

        public int? SummonMana { get; set; }

        public int? ConvinceMana { get; set; }

        public bool? SenseInvis { get; set; }

        public bool? ParaImmune { get; set; }

        public bool? Illusionable { get; set; }

        public bool? Pushable { get; set; }

        public bool? PushObjects { get; set; }

        [MaxLength(128)]
        public string? WalksThrough { get; set; }

        [MaxLength(128)]
        public string? WalksAround { get; set; }

        public int? RunsAt { get; set; }

        public int? Speed { get; set; }

        public string? Behaviour { get; set; }

        [MaxLength(128)]
        public string? AttackType { get; set; }

        [MaxLength(128)]
        public string? SpawnType { get; set; }

        public string? UsedElements { get; set; }

        public string? Location { get; set; }

        public string? Strategy { get; set; }

        public string? Notes { get; set; }

        public string? History { get; set; }

        [MaxLength(64)]
        public string? ImplementedVersion { get; set; }

        [MaxLength(64)]
        public string? RaceId { get; set; }

        public bool? UsesSpells { get; set; }

        [MaxLength(128)]
        public string? BestiaryClass { get; set; }

        [MaxLength(128)]
        public string? BestiaryDifficulty { get; set; }

        [MaxLength(128)]
        public string? BestiaryOccurrence { get; set; }

        [MaxLength(128)]
        public string? BosstiaryCategory { get; set; }

        public int? PrimaryAssetId { get; set; }

        [MaxLength(512)]
        public string? PrimaryImageStorageKey { get; set; }

        [MaxLength(256)]
        public string? PrimaryImageFileName { get; set; }

        [MaxLength(128)]
        public string? PrimaryImageMimeType { get; set; }

        public string ImagesJson { get; set; } = "[]";

        public string? SourceJson { get; set; }

        public string? StructuredDataJson { get; set; }

        [MaxLength(128)]
        public string ContentHash { get; set; } = string.Empty;

        public DateTimeOffset? LastSeenAt { get; set; }

        public DateTimeOffset SourceLastUpdatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public CreatureDamageModifiers Damage { get; set; } = new();

        public List<CreatureLootEntity> Loot { get; set; } = [];

        public List<CreatureSoundEntity> Sounds { get; set; } = [];

        public List<HuntingPlaceCreatureEntity> HuntingPlaces { get; set; } = [];

        public List<MonsterSpawnCreatureLinkEntity> SpawnLinks { get; set; } = [];

        public CreatureMonsterImageLinkEntity? MonsterImageLink { get; set; }
    }

    [Owned]
    public sealed class CreatureDamageModifiers
    {
        public decimal? PhysicalFactor { get; set; }
        public string? PhysicalRaw { get; set; }
        public decimal? FireFactor { get; set; }
        public string? FireRaw { get; set; }
        public decimal? IceFactor { get; set; }
        public string? IceRaw { get; set; }
        public decimal? EnergyFactor { get; set; }
        public string? EnergyRaw { get; set; }
        public decimal? EarthFactor { get; set; }
        public string? EarthRaw { get; set; }
        public decimal? HolyFactor { get; set; }
        public string? HolyRaw { get; set; }
        public decimal? DeathFactor { get; set; }
        public string? DeathRaw { get; set; }
        public decimal? HpDrainFactor { get; set; }
        public string? HpDrainRaw { get; set; }
        public decimal? DrownFactor { get; set; }
        public string? DrownRaw { get; set; }
        public decimal? HealFactor { get; set; }
        public string? HealRaw { get; set; }
    }

    [Table("CreatureLoots")]
    public sealed class CreatureLootEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CreatureId { get; set; }

        [ForeignKey(nameof(CreatureId))]
        public CreatureEntity? Creature { get; set; }

        [MaxLength(256)]
        public string ItemName { get; set; } = string.Empty;

        public int? MinAmount { get; set; }

        public int? MaxAmount { get; set; }

        [MaxLength(128)]
        public string? AmountRaw { get; set; }

        [MaxLength(64)]
        public string? Rarity { get; set; }

        [MaxLength(128)]
        public string? Chance { get; set; }

        public string? Raw { get; set; }
    }

    [Table("CreatureSounds")]
    public sealed class CreatureSoundEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CreatureId { get; set; }

        [ForeignKey(nameof(CreatureId))]
        public CreatureEntity? Creature { get; set; }

        [MaxLength(512)]
        public string Text { get; set; } = string.Empty;
    }
}
