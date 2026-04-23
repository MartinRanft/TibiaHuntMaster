using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Data.Entities.Character;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Tests.Analysis
{
    public sealed class ProgressInsightsServiceTests
    {
        private int _nextItemContentId = 1;

        [Fact]
        public async Task GetInsightsAsync_ShouldCalculateTrendAndLevelDelta_ForSelectedWindow()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();

            CharacterEntity character = new()
            {
                Name = "Tentakel",
                World = "Antica",
                Vocation = "Elder Druid",
                Level = 605
            };
            testDb.DbContext.Characters.Add(character);
            testDb.DbContext.Items.AddRange(
                CreateItem("knight armor", 65_000),
                CreateItem("giant sword", 17_000),
                CreateItem("blue robe", 10_000),
                CreateItem("crown armor", 50_000),
                CreateItem("gold coin", 1),
                CreateItem("platinum coin", 100),
                CreateItem("crystal coin", 10_000));
            await testDb.DbContext.SaveChangesAsync();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            testDb.DbContext.HuntSessions.AddRange(
                CreateSoloSession(character.Id, now.AddDays(-1), 2_000_000, 220_000, ("knight armor", 2), ("gold coin", 400)),
                CreateSoloSession(character.Id, now.AddDays(-2), 2_200_000, 250_000, ("giant sword", 5), ("platinum coin", 30)),
                CreateSoloSession(character.Id, now.AddDays(-9), 1_200_000, 120_000, ("blue robe", 3)),
                CreateSoloSession(character.Id, now.AddDays(-12), 1_100_000, 100_000, ("crown armor", 2)));
            testDb.DbContext.TeamHuntSessions.Add(
                CreateTeamSession(character.Id, now.AddDays(-3), 2_400_000, 280_000));
            testDb.DbContext.CharacterSnapshots.AddRange(
                CreateSnapshot(character.Id, now.AddDays(-12), 600),
                CreateSnapshot(character.Id, now.AddDays(-5), 603));
            await testDb.DbContext.SaveChangesAsync();

            ProgressInsightsService service = new(testDb.Factory);

            ProgressInsightsResult result = await service.GetInsightsAsync(character.Id, 605, InsightPeriod.Last7Days);

            result.SessionCount.Should().Be(3);
            result.XpPerHour.CurrentValue.Should().BeGreaterThan(result.XpPerHour.PreviousValue);
            result.XpPerHour.Direction.Should().Be(InsightTrendDirection.Up);
            result.BalancePerHunt.CurrentValue.Should().BeGreaterThan(result.BalancePerHunt.PreviousValue);
            result.BalancePerHunt.Direction.Should().Be(InsightTrendDirection.Up);
            result.LevelProgress.StartLevel.Should().Be(600);
            result.LevelProgress.CurrentLevel.Should().Be(605);
            result.LevelProgress.Delta.Should().Be(5);
            result.LevelProgress.HasBaseline.Should().BeTrue();
            result.LevelProgress.SnapshotCount.Should().Be(1);
            result.LevelTimeline.Should().NotBeEmpty();
            result.LevelTimeline.First().Level.Should().Be(600);
            result.LevelTimeline.Last().Level.Should().Be(605);
            result.DepotProgress.CurrentOpenValue.Should().Be(345_000);
            result.DepotProgress.ContributingSessions.Should().Be(4);
            result.DepotProgress.Timeline.Should().NotBeEmpty();
            result.DepotProgress.Timeline.First().Value.Should().Be(130_000);
            result.DepotProgress.Timeline.Last().Value.Should().Be(345_000);
            result.DepotProgress.RealizedValueInPeriod.Should().Be(0);
            result.DepotProgress.SaleCountInPeriod.Should().Be(0);
            result.HuntCadence.AverageHuntHoursPerDay.Should().BeApproximately(3d / 7d, 0.001d);
            result.HuntCadence.AverageSessionHours.Should().Be(1d);
            result.HuntCadence.ObservedDays.Should().Be(7);
            result.HasSkillData.Should().BeFalse();
        }

        [Fact]
        public async Task GetInsightsAsync_ShouldReturnNoDataTrend_WhenNoComparisonWindowExists()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();

            CharacterEntity character = new()
            {
                Name = "SoloOnly",
                World = "Antica",
                Vocation = "Knight",
                Level = 120
            };
            testDb.DbContext.Characters.Add(character);
            testDb.DbContext.Items.AddRange(
                CreateItem("knight armor", 70_000),
                CreateItem("gold coin", 1));
            await testDb.DbContext.SaveChangesAsync();

            testDb.DbContext.HuntSessions.Add(CreateSoloSession(
                character.Id,
                DateTimeOffset.UtcNow.AddDays(-2),
                900_000,
                50_000,
                ("knight armor", 1),
                ("gold coin", 250)));
            await testDb.DbContext.SaveChangesAsync();

            ProgressInsightsService service = new(testDb.Factory);

            ProgressInsightsResult result = await service.GetInsightsAsync(character.Id, 120, InsightPeriod.Last7Days);

            result.SessionCount.Should().Be(1);
            result.XpPerHour.Direction.Should().Be(InsightTrendDirection.NoData);
            result.BalancePerHunt.Direction.Should().Be(InsightTrendDirection.NoData);
            result.LevelProgress.HasBaseline.Should().BeFalse();
            result.LevelProgress.Delta.Should().Be(0);
            result.LevelTimeline.Should().BeEmpty();
            result.DepotProgress.CurrentOpenValue.Should().Be(70_000);
            result.DepotProgress.ContributingSessions.Should().Be(1);
            result.DepotProgress.Timeline.Should().HaveCount(2);
            result.DepotProgress.Timeline[0].Value.Should().Be(0);
            result.DepotProgress.Timeline[1].Value.Should().Be(70_000);
            result.DepotProgress.RealizedValueInPeriod.Should().Be(0);
            result.DepotProgress.SaleCountInPeriod.Should().Be(0);
        }

        [Fact]
        public async Task GetInsightsAsync_ShouldResetOpenDepotAfterSales_AndCalculateSellRhythm()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();

            CharacterEntity character = new()
            {
                Name = "Seller",
                World = "Antica",
                Vocation = "Knight",
                Level = 350
            };
            testDb.DbContext.Characters.Add(character);
            testDb.DbContext.Items.AddRange(
                CreateItem("falcon plate", 80_000),
                CreateItem("cobra boots", 130_000),
                CreateItem("lion hammer", 180_000),
                CreateItem("gnome sword", 280_000),
                CreateItem("soul orb", 160_000),
                CreateItem("platinum coin", 100));
            await testDb.DbContext.SaveChangesAsync();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            testDb.DbContext.HuntSessions.AddRange(
                CreateSoloSession(character.Id, now.AddDays(-20), 1_100_000, 80_000, ("falcon plate", 1)),
                CreateSoloSession(character.Id, now.AddDays(-15), 1_150_000, 130_000, ("cobra boots", 1), ("platinum coin", 200)),
                CreateSoloSession(character.Id, now.AddDays(-6), 1_300_000, 180_000, ("lion hammer", 1)),
                CreateSoloSession(character.Id, now.AddDays(-2), 1_450_000, 280_000, ("gnome sword", 1)),
                CreateSoloSession(character.Id, now.AddHours(-12), 1_550_000, 160_000, ("soul orb", 1)));
            testDb.DbContext.CharacterDepotSales.AddRange(
                CreateDepotSale(character.Id, now.AddDays(-10), 300_000),
                CreateDepotSale(character.Id, now.AddDays(-1), 550_000));
            await testDb.DbContext.SaveChangesAsync();

            ProgressInsightsService service = new(testDb.Factory);

            ProgressInsightsResult result = await service.GetInsightsAsync(character.Id, 350, InsightPeriod.Last30Days);

            result.DepotProgress.CurrentOpenValue.Should().Be(160_000);
            result.DepotProgress.ContributingSessions.Should().Be(1);
            result.DepotProgress.RealizedValueInPeriod.Should().Be(850_000);
            result.DepotProgress.SaleCountInPeriod.Should().Be(2);
            result.DepotProgress.LastSaleAtUtc.Should().BeCloseTo(now.AddDays(-1), TimeSpan.FromSeconds(1));
            result.DepotProgress.AverageDaysBetweenSales.Should().Be(9.0d);
            result.DepotProgress.RecentSales.Should().HaveCount(2);
            result.DepotProgress.Timeline.Should().Contain(point => point.Value == 0);
            result.DepotProgress.Timeline.Last().Value.Should().Be(160_000);
        }

        [Fact]
        public async Task GetInsightsAsync_ShouldExcludeBankCoinsFromOpenDepotValue()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();

            CharacterEntity character = new()
            {
                Name = "CoinTester",
                World = "Antica",
                Vocation = "Druid",
                Level = 200
            };

            testDb.DbContext.Characters.Add(character);
            testDb.DbContext.Items.AddRange(
                CreateItem("gold coin", 1),
                CreateItem("platinum coin", 100),
                CreateItem("crystal coin", 10_000));
            await testDb.DbContext.SaveChangesAsync();

            testDb.DbContext.HuntSessions.Add(CreateSoloSession(
                character.Id,
                DateTimeOffset.UtcNow.AddDays(-1),
                1_000_000,
                75_000,
                ("gold coin", 1_000),
                ("platinum coin", 50),
                ("crystal coin", 2)));
            await testDb.DbContext.SaveChangesAsync();

            ProgressInsightsService service = new(testDb.Factory);

            ProgressInsightsResult result = await service.GetInsightsAsync(character.Id, 200, InsightPeriod.Last7Days);

            result.DepotProgress.CurrentOpenValue.Should().Be(0);
            result.DepotProgress.ContributingSessions.Should().Be(0);
            result.DepotProgress.Timeline.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteDepotSaleAsync_ShouldRemoveStoredSale()
        {
            await using TestDatabaseContext testDb = await TestDatabaseContext.CreateAsync();

            CharacterEntity character = new()
            {
                Name = "UndoSeller",
                World = "Antica",
                Vocation = "Knight",
                Level = 500
            };

            testDb.DbContext.Characters.Add(character);
            testDb.DbContext.Items.Add(CreateItem("pair of dreamwalkers", 250_000));
            await testDb.DbContext.SaveChangesAsync();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            testDb.DbContext.HuntSessions.Add(CreateSoloSession(
                character.Id,
                now.AddDays(-2),
                2_000_000,
                300_000,
                ("pair of dreamwalkers", 1)));

            CharacterDepotSaleEntity sale = CreateDepotSale(character.Id, now.AddDays(-1), 250_000);
            testDb.DbContext.CharacterDepotSales.Add(sale);
            await testDb.DbContext.SaveChangesAsync();

            ProgressInsightsService service = new(testDb.Factory);

            await service.DeleteDepotSaleAsync(character.Id, sale.Id);

            ProgressInsightsResult result = await service.GetInsightsAsync(character.Id, 500, InsightPeriod.Last30Days);

            result.DepotProgress.RecentSales.Should().BeEmpty();
            result.DepotProgress.RealizedValueInPeriod.Should().Be(0);
            result.DepotProgress.CurrentOpenValue.Should().Be(250_000);
        }

        private static HuntSessionEntity CreateSoloSession(
            int characterId,
            DateTimeOffset timestamp,
            long xpPerHour,
            long balance,
            params (string Name, int Amount)[] lootItems)
        {
            return new HuntSessionEntity
            {
                CharacterId = characterId,
                SessionStartTime = timestamp,
                ImportedAt = timestamp,
                Duration = TimeSpan.FromHours(1),
                XpPerHour = xpPerHour,
                XpGain = xpPerHour,
                Balance = balance,
                Loot = balance + 20_000,
                Supplies = 20_000,
                LootItems = lootItems.Select(item => new HuntLootEntry
                {
                    ItemName = item.Name,
                    Amount = item.Amount
                }).ToList(),
                RawInput = "test"
            };
        }

        private static TeamHuntSessionEntity CreateTeamSession(int characterId, DateTimeOffset timestamp, long xpPerHour, long totalBalance)
        {
            return new TeamHuntSessionEntity
            {
                CharacterId = characterId,
                SessionStartTime = timestamp,
                ImportedAt = timestamp,
                Duration = TimeSpan.FromHours(1),
                XpPerHour = xpPerHour,
                XpGain = xpPerHour,
                TotalBalance = totalBalance,
                TotalLoot = totalBalance + 40_000,
                TotalSupplies = 40_000,
                RawInput = "test"
            };
        }

        private static CharacterSnapshotEntity CreateSnapshot(int characterId, DateTimeOffset timestamp, int level)
        {
            return new CharacterSnapshotEntity
            {
                CharacterId = characterId,
                FetchedAtUtc = timestamp,
                RawJson = $"{{\"character\":{{\"character\":{{\"level\":{level}}}}}}}"
            };
        }

        private static CharacterDepotSaleEntity CreateDepotSale(int characterId, DateTimeOffset timestamp, long realizedValue)
        {
            return new CharacterDepotSaleEntity
            {
                CharacterId = characterId,
                SoldAtUtc = timestamp,
                RealizedValue = realizedValue,
                CreatedAtUtc = timestamp
            };
        }

        private ItemEntity CreateItem(string name, long value)
        {
            return new ItemEntity
            {
                ContentId = _nextItemContentId++,
                Name = name,
                ActualName = name,
                NormalizedName = name.Trim().ToUpperInvariant(),
                Value = value,
                SellTo = "Rashid"
            };
        }

        private sealed class TestDatabaseContext : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;

            private TestDatabaseContext(SqliteConnection connection, AppDbContext dbContext, IDbContextFactory<AppDbContext> factory)
            {
                _connection = connection;
                DbContext = dbContext;
                Factory = factory;
            }

            public AppDbContext DbContext { get; }

            public IDbContextFactory<AppDbContext> Factory { get; }

            public static async Task<TestDatabaseContext> CreateAsync()
            {
                SqliteConnection connection = new("DataSource=:memory:");
                await connection.OpenAsync();

                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite(connection)
                                                         .Options;

                AppDbContext dbContext = new(options);
                await dbContext.Database.EnsureCreatedAsync();

                return new TestDatabaseContext(connection, dbContext, new TestDbContextFactory(connection));
            }

            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }

        private sealed class TestDbContextFactory(SqliteConnection connection) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite(connection)
                                                         .Options;

                return new AppDbContext(options);
            }

            public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CreateDbContext());
            }
        }
    }
}
