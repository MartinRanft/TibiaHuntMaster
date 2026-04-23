using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterAccountEntityConfig : IEntityTypeConfiguration<CharacterAccountEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterAccountEntity> e)
        {
            e.ToTable("CharacterAccounts");
            e.HasKey(x => x.Id);
            e.HasOne(a => a.Character)
             .WithOne(c => c.Account)
             .HasForeignKey<CharacterAccountEntity>(a => a.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Created).HasMaxLength(64);
            e.Property(x => x.LoyaltyTitle).HasMaxLength(64);
            e.Property(x => x.Position).HasMaxLength(64);
        }
    }
}