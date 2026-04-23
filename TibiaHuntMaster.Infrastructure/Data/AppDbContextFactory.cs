using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TibiaHuntMaster.Infrastructure.Data
{
    /// <summary>
    ///     Diese Factory wird NUR von den EF Core CLI Tools (dotnet ef migrations...) verwendet.
    ///     Sie wird NICHT zur Laufzeit der App genutzt.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

            // Wir nutzen hier einen Platzhalter-Namen. 
            // Für die Erstellung der Migration (Code-Generierung) ist der Pfad egal.
            optionsBuilder.UseSqlite("Data Source=tibia_migration_placeholder.db");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}