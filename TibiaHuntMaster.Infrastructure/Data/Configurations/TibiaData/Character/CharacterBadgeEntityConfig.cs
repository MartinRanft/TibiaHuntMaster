using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterBadgeEntityConfig : IEntityTypeConfiguration<CharacterBadgeEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterBadgeEntity> e)
        {
            e.ToTable("CharacterBadges");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new
            {
                x.CharacterId,
                x.Name
            }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(256);
            e.Property(x => x.IconUrl).HasMaxLength(256);
        }
    }
}