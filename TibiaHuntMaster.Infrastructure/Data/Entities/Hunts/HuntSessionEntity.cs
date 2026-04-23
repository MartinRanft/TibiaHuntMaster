using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    [Table("HuntSessions")]
    public sealed class HuntSessionEntity
    {
        [Key]public int Id { get; set; }

        // Wir verknüpfen jeden Hunt mit einem Charakter in unserer DB
        public int CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))]public CharacterEntity Character { get; set; } = null!;

        public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset SessionStartTime { get; set; }

        // --- Werte aus dem Tibia Client ---
        public TimeSpan Duration { get; set; }

        public long XpGain { get; set; }

        public long? RawXpGain { get; set; }

        public long XpPerHour { get; set; }

        public long Loot { get; set; }

        public long Supplies { get; set; }

        public long Balance { get; set; } // Kann negativ sein (Waste)

        public long Damage { get; set; }

        public long Healing { get; set; }

        // Event Flags & Notizen
        public bool IsDoubleXp { get; set; }

        public bool IsDoubleLoot { get; set; }

        public bool IsRapidRespawn { get; set; }

        public int? XpBoostPercent { get; set; }

        public int? XpBoostActiveMinutes { get; set; }

        public int? CustomXpRatePercent { get; set; }

        public int? HuntGroupId { get; set; }

        public int? HuntingPlaceId { get; set; }

        public bool IgnoreLootVerificationWarning { get; set; }

        public bool IgnoreXpVerificationWarning { get; set; }

        public string? Notes { get; set; }

        // Listen
        public List<HuntLootEntry> LootItems { get; set; } = new();

        public List<HuntMonsterEntry> KilledMonsters { get; set; } = new();

        public HuntGroupEntity? Group { get; set; }

        // NEU: Liste der Anpassungen
        public List<HuntSupplyAdjustment> SupplyAdjustments { get; set; } = new();

        // Zur Sicherheit speichern wir den Original-Text
        public string RawInput { get; set; } = string.Empty;
    }
}
