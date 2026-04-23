using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class TeamHuntSessionEntityConfig : IEntityTypeConfiguration<TeamHuntSessionEntity>
    {
        public void Configure(EntityTypeBuilder<TeamHuntSessionEntity> builder)
        {
            builder.ToTable("TeamHuntSessions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ImportedAt)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            builder.Property(x => x.SessionStartTime)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            builder.Property(x => x.Duration)
                   .HasConversion(new TimeSpanToTicksConverter());

            builder.HasMany(s => s.Members)
                   .WithOne(m => m.Session)
                   .HasForeignKey(m => m.TeamHuntSessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Character)
                   .WithMany()
                   .HasForeignKey(x => x.CharacterId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}