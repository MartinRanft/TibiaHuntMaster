using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Imbuement
{
    public class ImbuementProfileEntityConfig : IEntityTypeConfiguration<ImbuementProfileEntity>
    {
        public void Configure(EntityTypeBuilder<ImbuementProfileEntity> builder)
        {
            builder.ToTable("ImbuementProfiles");
            builder.HasKey(x => x.Id);

            // Converter für das neue Feld hinzufügen:
            builder.Property(x => x.LastUpdated)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            builder.HasOne(x => x.Character)
                   .WithMany()
                   .HasForeignKey(x => x.CharacterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ActiveImbuements)
                   .WithOne()
                   .HasForeignKey(x => x.ImbuementProfileId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // CharacterActiveImbuementConfig bleibt wie vorher...
    public class CharacterActiveImbuementConfig : IEntityTypeConfiguration<CharacterActiveImbuement>
    {
        public void Configure(EntityTypeBuilder<CharacterActiveImbuement> builder)
        {
            builder.ToTable("CharacterActiveImbuements");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Recipe)
                   .WithMany()
                   .HasForeignKey(x => x.ImbuementRecipeId);
        }
    }
}