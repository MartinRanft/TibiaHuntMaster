using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Character;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Character
{
    public sealed class CharacterDepotSaleEntityConfig : IEntityTypeConfiguration<CharacterDepotSaleEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterDepotSaleEntity> builder)
        {
            builder.ToTable("CharacterDepotSales");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.SoldAtUtc)
                   .HasConversion(new DateTimeOffsetToBinaryConverter())
                   .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                   .HasConversion(new DateTimeOffsetToBinaryConverter())
                   .IsRequired();

            builder.Property(x => x.RealizedValue)
                   .IsRequired();

            builder.HasIndex(x => new { x.CharacterId, x.SoldAtUtc });

            builder.HasOne(x => x.Character)
                   .WithMany()
                   .HasForeignKey(x => x.CharacterId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
