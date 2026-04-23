using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Content
{
    [Index(nameof(ContentId), IsUnique = true)]
    [Table("Items")]
    public sealed class ItemEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ContentId { get; set; }

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string NormalizedName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string ActualName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Plural { get; set; } = string.Empty;

        [MaxLength(64)]
        public string Article { get; set; } = string.Empty;

        [MaxLength(64)]
        public string Implemented { get; set; } = string.Empty;

        [MaxLength(512)]
        public string Icon { get; set; } = string.Empty;

        public int? ItemIdPrimary { get; set; }

        public string ItemIdsCsv { get; set; } = string.Empty;

        [MaxLength(128)]
        public string CategorySlug { get; set; } = string.Empty;

        [MaxLength(128)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(64)]
        public string TemplateType { get; set; } = string.Empty;

        [MaxLength(128)]
        public string ObjectClass { get; set; } = string.Empty;

        [MaxLength(128)]
        public string PrimaryType { get; set; } = string.Empty;

        [MaxLength(128)]
        public string SecondaryType { get; set; } = string.Empty;

        [MaxLength(64)]
        public string WeaponType { get; set; } = string.Empty;

        [MaxLength(32)]
        public string Hands { get; set; } = string.Empty;

        public int? LevelRequired { get; set; }

        public int? Attack { get; set; }

        public int? Defense { get; set; }

        public int? DefenseMod { get; set; }

        public int? Armor { get; set; }

        public int? Range { get; set; }

        public int? ImbueSlots { get; set; }

        [MaxLength(64)]
        public string VocRequired { get; set; } = string.Empty;

        [MaxLength(64)]
        public string DamageType { get; set; } = string.Empty;

        [MaxLength(64)]
        public string DamageRange { get; set; } = string.Empty;

        public int? ElementAttack { get; set; }

        public int? EnergyAttack { get; set; }

        public int? FireAttack { get; set; }

        public int? EarthAttack { get; set; }

        public int? IceAttack { get; set; }

        public int? DeathAttack { get; set; }

        public int? HolyAttack { get; set; }

        [MaxLength(256)]
        public string ResistSummary { get; set; } = string.Empty;

        public bool? Stackable { get; set; }

        public bool? Usable { get; set; }

        public bool? Pickupable { get; set; }

        public bool? Marketable { get; set; }

        public bool? Walkable { get; set; }

        public decimal? WeightOz { get; set; }

        public long? NpcPrice { get; set; }

        public long? NpcValue { get; set; }

        [MaxLength(256)]
        public string SellTo { get; set; } = string.Empty;

        public long Value { get; set; }

        [MaxLength(512)]
        public string Attrib { get; set; } = string.Empty;

        [MaxLength(128)]
        public string UpgradeClass { get; set; } = string.Empty;

        public string DroppedByCsv { get; set; } = string.Empty;

        public string SoundsJson { get; set; } = "[]";

        public string Notes { get; set; } = string.Empty;

        public string FlavorText { get; set; } = string.Empty;

        public int? PrimaryAssetId { get; set; }

        [MaxLength(512)]
        public string PrimaryImageStorageKey { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PrimaryImageFileName { get; set; } = string.Empty;

        [MaxLength(128)]
        public string PrimaryImageMimeType { get; set; } = string.Empty;

        public string ImagesJson { get; set; } = "[]";

        [MaxLength(512)]
        public string WikiUrl { get; set; } = string.Empty;

        public DateTimeOffset? LastSeenAt { get; set; }

        public DateTimeOffset SourceLastUpdatedAt { get; set; }

        public string ExtrasJson { get; set; } = "{}";

        public string SourceJson { get; set; } = "{}";

        [MaxLength(128)]
        public string ContentHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
