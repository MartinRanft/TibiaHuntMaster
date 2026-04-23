using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class HuntMonsterEntryConfig : IEntityTypeConfiguration<HuntMonsterEntry>
    {
        public void Configure(EntityTypeBuilder<HuntMonsterEntry> builder)
        {
            builder.ToTable("HuntMonsterEntries");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MonsterName).HasMaxLength(100).IsRequired();

            builder.HasOne<HuntSessionEntity>()
                   .WithMany(s => s.KilledMonsters)
                   .HasForeignKey(x => x.HuntSessionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}