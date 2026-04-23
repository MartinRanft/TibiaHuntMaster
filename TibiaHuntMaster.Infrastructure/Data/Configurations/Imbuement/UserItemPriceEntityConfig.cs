using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Imbuement
{
    public class UserItemPriceEntityConfig : IEntityTypeConfiguration<UserItemPriceEntity>
    {
        public void Configure(EntityTypeBuilder<UserItemPriceEntity> builder)
        {
            builder.ToTable("UserItemPrices");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Price).IsRequired();

            builder.Property(x => x.LastUpdated)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            // Unique Index: Ein Item hat nur einen Preis pro User (hier global, später pro Server denkbar)
            builder.HasIndex(x => x.ItemId).IsUnique();

            builder.HasOne(x => x.Item)
                   .WithMany()
                   .HasForeignKey(x => x.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}