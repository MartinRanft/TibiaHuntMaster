using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class HuntSupplyAdjustmentConfig : IEntityTypeConfiguration<HuntSupplyAdjustment>
    {
        public void Configure(EntityTypeBuilder<HuntSupplyAdjustment> builder)
        {
            // Tabellenname
            builder.ToTable("HuntSupplyAdjustments");

            // Primary Key
            builder.HasKey(x => x.Id);

            // Properties
            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Value)
                   .IsRequired();

            // Enum: Wir speichern es standardmäßig als Integer (0 = Addition, 1 = Deduction).
            // Das ist performanter für SQLite.
            builder.Property(x => x.Type)
                   .IsRequired();

            // Beziehung zur Session (1:n)
            // Wenn die Session gelöscht wird, verschwinden auch die manuellen Anpassungen (Cascade).
            builder.HasOne<HuntSessionEntity>()
                   .WithMany(s => s.SupplyAdjustments)
                   .HasForeignKey(x => x.HuntSessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}