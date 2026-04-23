using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterHouseEntityConfig : IEntityTypeConfiguration<CharacterHouseEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterHouseEntity> e)
        {
            e.ToTable("CharacterHouses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new
            {
                x.CharacterId,
                x.HouseId
            }).IsUnique();
            e.Property(x => x.HouseId).IsRequired();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Town).HasMaxLength(64);
            e.Property(x => x.Paid).HasMaxLength(64);
        }
    }
}