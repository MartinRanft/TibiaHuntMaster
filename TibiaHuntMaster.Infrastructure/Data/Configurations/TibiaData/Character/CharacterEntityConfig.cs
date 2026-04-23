using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterEntityConfig : IEntityTypeConfiguration<CharacterEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterEntity> e)
        {
            e.ToTable("Characters");
            e.HasKey(x => x.Id);

            e.HasIndex(x => new
            {
                x.Name,
                x.World
            }).IsUnique();

            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.World).HasMaxLength(64).IsRequired();
            e.Property(x => x.Vocation).HasMaxLength(64).IsRequired();

            e.Property(x => x.GuildName).HasMaxLength(100);
            e.Property(x => x.Residence).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.AccountStatus).HasMaxLength(50);

            e.Property(x => x.Sex).HasMaxLength(16);
            e.Property(x => x.LastUpdatedUtc)
             .HasConversion(new DateTimeOffsetToBinaryConverter())
             .IsRequired();

            e.Property(x => x.LastLogin)
             .HasConversion(new DateTimeOffsetToBinaryConverter());

            // 1:1 Account
            e.HasOne(x => x.Account)
             .WithOne(x => x.Character)
             .HasForeignKey<CharacterAccountEntity>(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);

            // 1:n Navigationen – Cascade ist hier ok, da abgeleitete Daten
            e.HasMany(x => x.Badges)
             .WithOne(x => x.Character)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Achievements)
             .WithOne(x => x.Character)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Houses)
             .WithOne(x => x.Character)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Deaths)
             .WithOne(x => x.Character)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Snapshots)
             .WithOne(x => x.Character)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}