using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class HuntSessionEntityConfig : IEntityTypeConfiguration<HuntSessionEntity>
    {
        public void Configure(EntityTypeBuilder<HuntSessionEntity> builder)
        {
            // Die Tabelle explizit benennen (hattest du vorher per Attribute, hier ist es sauberer)
            builder.ToTable("HuntSessions");

            builder.HasKey(x => x.Id);

            // WICHTIG: Konvertierung für SQLite Sortierung!
            // Speichert das Datum als 'long' (Zahl), was SQLite sortieren kann.
            builder.Property(x => x.ImportedAt)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            builder.Property(x => x.SessionStartTime)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            // Optional: Für Duration (TimeSpan) ist Ticks auch besser als String
            builder.Property(x => x.Duration)
                   .HasConversion(new TimeSpanToTicksConverter());

            // Beziehung konfigurieren (Optional, da EF das meist erkennt, aber explizit ist besser)
            builder.HasOne(x => x.Character)
                   .WithMany() // Character hat keine Liste von Sessions definiert? Wenn doch, hier .WithMany(c => c.Sessions)
                   .HasForeignKey(x => x.CharacterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.HuntGroupId);
            builder.HasIndex(x => x.HuntingPlaceId);
        }
    }
}
