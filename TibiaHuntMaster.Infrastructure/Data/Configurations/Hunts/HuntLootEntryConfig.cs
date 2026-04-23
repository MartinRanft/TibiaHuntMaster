using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class HuntLootEntryConfig : IEntityTypeConfiguration<HuntLootEntry>
    {
        public void Configure(EntityTypeBuilder<HuntLootEntry> builder)
        {
            builder.ToTable("HuntLootEntries");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ItemName).HasMaxLength(100).IsRequired();

            // Beziehung zur Session
            builder.HasOne<HuntSessionEntity>()
                   .WithMany(s => s.LootItems)
                   .HasForeignKey(x => x.HuntSessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}