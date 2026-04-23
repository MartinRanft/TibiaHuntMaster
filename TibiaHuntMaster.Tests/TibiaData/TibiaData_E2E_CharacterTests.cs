using System.Net;

using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Core.Characters;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Http.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.TibiaData;

using Xunit.Abstractions;

namespace TibiaHuntMaster.Tests.TibiaData
{
    public sealed class TibiaDataE2ECharacterTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;

        private static (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) SetupDb()
        {
            SqliteConnection connection = new("DataSource=:memory:");
            connection.Open(); // WICHTIG: Verbindung offen lassen!

            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connection)
                                                     .EnableSensitiveDataLogging()
                                                     .Options;

            using AppDbContext db = new(options);
            db.Database.EnsureCreated();

            return (new TestDbContextFactory(options), connection);
        }

        [Fact(DisplayName = "🌐 LIVE: TibiaData returns data for 'Tentakel' (optional)")]
        [Trait("Category", "Online")]
        public async Task Live_TibiaData_Returns_Tentakel()
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
            HttpClient httpClient = new();
            TibiaDataClient client = new(httpClient);

            TibiaDataCharacterResponse? res = await client.GetCharactersAsync("Tentakel", cts.Token);

            res.Should().NotBeNull();
            res!.Information.Status.HttpCode.Should().Be((int)HttpStatusCode.OK);
            res.Character.Character.Name.Should().Be("Tentakel");
        }

        [Fact(DisplayName = "💾 DB: Import+Save persists full character graph (LIVE)")]
        [Trait("Category", "Online")]
        public async Task Live_DB_Persists_FullGraph()
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(25));
            (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) = SetupDb(); // Factory holen
            try
            {
                using HttpClient http = new()
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };
                TibiaDataClient client = new(http);

                // Service mit Factory erstellen
                CharacterService svc = new(client, factory);

                // 1) Import
                Character domain = await svc.ImportFromTibiaDataAsync("Tentakel", cts.Token);
                domain.Should().NotBeNull();

                // 2) Save
                await svc.SaveAsync(domain, cts.Token);

                // 3) Reload (neuer Context via Factory oder manuell für Assert)
                await using AppDbContext db = await factory.CreateDbContextAsync(cts.Token);
                CharacterEntity? saved = await db.Characters
                                                 .Include(c => c.Badges)
                                                 .Include(c => c.Achievements)
                                                 .Include(c => c.Houses)
                                                 .Include(c => c.Deaths)
                                                 .Include(c => c.Account)
                                                 .FirstOrDefaultAsync(c => c.Name == domain.Name && c.World == domain.World, cts.Token);

                saved.Should().NotBeNull();
                saved!.Name.Should().Be(domain.Name);
                saved.Badges.Select(b => b.Name).Should().OnlyHaveUniqueItems();
            }
            finally
            {
                connection.Dispose();
            }
        }

        [Fact(DisplayName = "🔁 DB: Re-import + Save does NOT duplicate children (LIVE)")]
        [Trait("Category", "Online")]
        public async Task Live_DB_Upsert_DoesNot_Duplicate_Children()
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(35));
            (IDbContextFactory<AppDbContext> factory, SqliteConnection connection) = SetupDb();
            try
            {
                using HttpClient http = new()
                {
                    Timeout = TimeSpan.FromSeconds(20)
                };
                TibiaDataClient client = new(http);
                CharacterService svc = new(client, factory);

                // First import + save
                Character d1 = await svc.ImportFromTibiaDataAsync("Tentakel", cts.Token);
                await svc.SaveAsync(d1, cts.Token);

                // Second import + save
                Character d2 = await svc.ImportFromTibiaDataAsync("Tentakel", cts.Token);
                await svc.SaveAsync(d2, cts.Token);

                // Load check
                await using AppDbContext db = await factory.CreateDbContextAsync(cts.Token);
                CharacterEntity saved = await db.Characters
                                                .Include(c => c.Badges)
                                                .Include(c => c.Achievements)
                                                .Include(c => c.Houses)
                                                .Include(c => c.Deaths)
                                                .SingleAsync(c => c.Name == d2.Name && c.World == d2.World, cts.Token);

                saved.Badges.Select(b => b.Name).Should().OnlyHaveUniqueItems();
            }
            finally
            {
                connection.Dispose();
            }
        }

        // Helper-Klasse für Tests
        private class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                return new AppDbContext(options);
            }
            public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            {
                return Task.FromResult(new AppDbContext(options));
            }
        }
    }
}