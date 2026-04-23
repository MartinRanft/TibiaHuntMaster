using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Character
{
    [Table("CharacterDepotSales")]
    public sealed class CharacterDepotSaleEntity
    {
        [Key] public int Id { get; set; }

        public int CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))] public CharacterEntity Character { get; set; } = null!;

        public DateTimeOffset SoldAtUtc { get; set; }

        public long RealizedValue { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
