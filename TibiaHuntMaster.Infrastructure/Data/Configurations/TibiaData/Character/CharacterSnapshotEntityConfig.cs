// CharacterSnapshotEntityConfig

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.TibiaData.Character
{
    public sealed class CharacterSnapshotEntityConfig : IEntityTypeConfiguration<CharacterSnapshotEntity>
    {
        public void Configure(EntityTypeBuilder<CharacterSnapshotEntity> e)
        {
            e.ToTable("CharacterSnapshots");
            e.HasKey(x => x.Id);

            // WICHTIG: Converter für SQLite
            e.Property(x => x.FetchedAtUtc)
             .IsRequired()
             .HasConversion(new DateTimeOffsetToBinaryConverter()); // robust für Sortierung/Index

            e.Property(x => x.RawJson)
             .HasColumnType("TEXT")
             .IsRequired();

            // optional für cap/Sortier-Queries:
            e.HasIndex(x => new
            {
                x.CharacterId,
                x.FetchedAtUtc
            });
        }
    }
}