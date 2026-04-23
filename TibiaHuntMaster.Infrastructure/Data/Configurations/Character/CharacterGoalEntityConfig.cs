using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Character;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Character
{
    public class CharacterGoalEntityConfig : IEntityTypeConfiguration<CharacterGoalEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterGoalEntity> builder)
        {
            builder.ToTable("CharacterGoals");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Type).IsRequired();

            // Datum konvertieren
            builder.Property(x => x.CreatedAt)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            builder.HasOne(x => x.Character)
                   .WithMany()
                   .HasForeignKey(x => x.CharacterId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}