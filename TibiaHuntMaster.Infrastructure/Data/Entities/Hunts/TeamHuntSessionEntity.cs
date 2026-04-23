using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    [Table("TeamHuntSessions")]
    public sealed class TeamHuntSessionEntity
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))]public CharacterEntity Character { get; set; } = null!;

        public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset SessionStartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string LootType { get; set; } = string.Empty;

        public long TotalLoot { get; set; }

        public long TotalSupplies { get; set; }

        public long TotalBalance { get; set; }

        public long XpGain { get; set; }

        public long XpPerHour { get; set; }

        public string? Notes { get; set; }

        public List<TeamHuntMemberEntity> Members { get; set; } = new();

        public string RawInput { get; set; } = string.Empty;
    }
}