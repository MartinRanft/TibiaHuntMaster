using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement
{
    [Table("UserItemPrices")]
    public sealed class UserItemPriceEntity
    {
        [Key]public int Id { get; set; }

        public int ItemId { get; set; }

        [ForeignKey(nameof(ItemId))]public ItemEntity Item { get; set; } = null!;

        // Der vom User festgelegte Marktpreis
        public long Price { get; set; }

        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    }
}