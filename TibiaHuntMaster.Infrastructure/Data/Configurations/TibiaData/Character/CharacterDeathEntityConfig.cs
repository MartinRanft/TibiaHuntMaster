using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterDeathEntityConfig : IEntityTypeConfiguration<CharacterDeathEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterDeathEntity> e)
        {
            e.ToTable("CharacterDeaths");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new
            {
                x.CharacterId,
                x.TimeUtc
            }).IsUnique();
            e.Property(x => x.TimeUtc).IsRequired();
            e.Property(x => x.Level).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(256);

            // JSON-Blob für Killers/Assists
            e.Property(x => x.KillersJson).HasColumnType("TEXT").IsRequired();
        }
    }
}