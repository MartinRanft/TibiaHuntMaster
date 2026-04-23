using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterAchievementEntityConfig : IEntityTypeConfiguration<CharacterAchievementEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterAchievementEntity> e)
        {
            e.ToTable("CharacterAchievements");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new
            {
                x.CharacterId,
                x.Name
            }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Grade).IsRequired();
            e.Property(x => x.Secret).IsRequired();
        }
    }
}