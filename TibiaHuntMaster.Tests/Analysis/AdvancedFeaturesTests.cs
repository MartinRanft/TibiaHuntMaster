using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Analysis;
using TibiaHuntMaster.Infrastructure.Services.Parsing;
using CreatureEntitys = TibiaHuntMaster.Infrastructure.Data.Entities.Content.CreatureEntity;
using CreatureLoot = TibiaHuntMaster.Infrastructure.Data.Entities.Content.CreatureLootEntity;

using Xunit.Abstractions;

namespace TibiaHuntMaster.Tests.Analysis
{
    public sealed class AdvancedFeaturesTests(ITestOutputHelper output)
    {
        private readonly ITestOutputHelper _output = output;
        private int _nextCreatureContentId = 1;

        private (IDbContextFactory<AppDbContext>, SqliteConnection) GetFactory()
        {
            SqliteConnection conn = new("DataSource=:memory:");
            conn.Open();
            DbContextOptions<AppDbContext> opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;

            using AppDbContext db = new(opts);
            db.Database.EnsureCreated();

            return (new TestDbContextFactory(opts), conn);
        }

        [Fact(DisplayName = "💰 Smart Loot: Groups Items by Priority Vendor")]
        public async Task SmartLoot_Groups_By_Vendor()
        {
            (IDbContextFactory<AppDbContext> factory, SqliteConnection conn) = GetFactory();
            try
            {
                // Daten seeden (wir brauchen kurz einen Context dafür)
                await using (AppDbContext db = await factory.CreateDbContextAsync())
                {
                    db.Characters.Add(new CharacterEntity
                    {
                        Name = "LootChar",
                        World = "Antica",
                        Vocation = "None",
                        LastUpdatedUtc = DateTimeOffset.UtcNow
                    });

                    db.Items.Add(new ItemEntity
                    {
                        ContentId = 1,
                        Name = "Platinum Coin",
                        NormalizedName = "PLATINUM COIN",
                        SellTo = "Banker, Market",
                        Value = 100,
                        WeightOz = 0.1m
                    });
                    db.Items.Add(new ItemEntity
                    {
                        ContentId = 2,
                        Name = "Plate Armor",
                        NormalizedName = "PLATE ARMOR",
                        SellTo = "Hardek, Sam, Shanar",
                        Value = 400,
                        WeightOz = 120.0m
                    });
                    db.Items.Add(new ItemEntity
                    {
                        ContentId = 3,
                        Name = "Blue Robe",
                        NormalizedName = "BLUE ROBE",
                        SellTo = "Nah'bob",
                        Value = 10000,
                        WeightOz = 22.0m
                    });
                    db.Items.Add(new ItemEntity
                    {
                        ContentId = 4,
                        Name = "Giant Shimmering Pearl",
                        NormalizedName = "GIANT SHIMMERING PEARL",
                        SellTo = "Briasol",
                        Value = 0,
                        NpcValue = 3000,
                        WeightOz = 1.0m
                    });

                    await db.SaveChangesAsync();

                    HuntSessionEntity session = new()
                    {
                        CharacterId = 1,
                        ImportedAt = DateTimeOffset.UtcNow
                    };
                    db.HuntSessions.Add(session);
                    await db.SaveChangesAsync();

                    db.HuntLootEntries.Add(new HuntLootEntry
                    {
                        HuntSessionId = session.Id,
                        ItemName = "platinum coin",
                        Amount = 5
                    });
                    db.HuntLootEntries.Add(new HuntLootEntry
                    {
                        HuntSessionId = session.Id,
                        ItemName = "plate armor",
                        Amount = 2
                    });
                    db.HuntLootEntries.Add(new HuntLootEntry
                    {
                        HuntSessionId = session.Id,
                        ItemName = "blue robe",
                        Amount = 1
                    });
                    db.HuntLootEntries.Add(new HuntLootEntry
                    {
                        HuntSessionId = session.Id,
                        ItemName = "giant shimmering pearl",
                        Amount = 2
                    });
                    await db.SaveChangesAsync();
                }

                // Service testen (kriegt jetzt Factory)
                LootAnalysisService service = new(factory);
                List<LootGroup> result = await service.AnalyzeSessionLootAsync(1);

                // Assert
                result.Should().Contain(g => g.Vendor == "Blue Djinn");
                result.Should().Contain(g => g.Vendor.Contains("Hardek"));
                result.SelectMany(group => group.Items)
                      .Should()
                      .Contain(item => item.Name == "giant shimmering pearl" && item.TotalValue == 6000);
            }
            finally
            {
                conn.Dispose();
            }
        }

        [Fact(DisplayName = "🔍 Search: Finds Creatures by Drop")]
        public async Task Search_Finds_Creatures()
        {
            (IDbContextFactory<AppDbContext> factory, SqliteConnection conn) = GetFactory();
            try
            {
                await using (AppDbContext db = await factory.CreateDbContextAsync())
                {
                    List<CreatureEntitys> creatures =
                    [
                        CreateCreature("Demon", "Magic Plate Armor"),
                        CreateCreature("Giant Spider", "Plate Armor"),
                        CreateCreature("Valkyrie", "Plate Armor"),
                        CreateCreature("Black Knight", "Plate Armor"),
                        CreateCreature("Orc Warlord", "Plate Armor"),
                        CreateCreature("Rat", "Cheese")
                    ];
                    db.Creatures.AddRange(creatures);
                    await db.SaveChangesAsync();
                }

                // Service mit Factory
                GlossarySearchService service = new(factory);
                string searchTerm = "Plate Armor";
                List<CreatureEntitys> droppers = await service.FindCreaturesDroppingAsync(searchTerm);

                droppers.Should().HaveCount(5);
                droppers.Should().Contain(c => c.Name == "Demon");
            }
            finally
            {
                conn.Dispose();
            }
        }

        private CreatureEntitys CreateCreature(string name, string drop)
        {
            CreatureEntitys c = new()
            {
                ContentId = _nextCreatureContentId++,
                Name = name,
                ActualName = name
            };
            c.Loot.Add(new CreatureLoot
            {
                ItemName = drop
            });
            return c;
        }

        [Fact(DisplayName = "🤖 Detector: Identifies Logs")]
        public void Detector_Identifies_Logs()
        {
            // Unverändert, da keine DB involviert
            LogDetectorService detector = new(
                new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance),
                new TeamHuntParser(NullLogger<TeamHuntParser>.Instance));

            string soloLog = "Session data: ...\nXP Gain: 1000\nBalance: 500";
            string teamLog = "Session data: ...\nLoot Type: Leader\nBalance: 1000\nPlayer1\n  Balance: 500";
            string trash = "Hello World";

            detector.DetectType(soloLog).Should().Be(DetectedLogType.SoloHunt);
            detector.DetectType(teamLog).Should().Be(DetectedLogType.TeamHunt);
            detector.DetectType(trash).Should().Be(DetectedLogType.None);
        }

        // Wieder unsere kleine Helper Factory
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
