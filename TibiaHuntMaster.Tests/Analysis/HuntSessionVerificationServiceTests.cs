using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Content;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.Tests.Analysis
{
    public sealed class HuntSessionVerificationServiceTests
    {
        [Fact(DisplayName = "🧮 Verification: Detects loot mismatch and allows correction when all items match")]
        public async Task VerifyAsync_ShouldDetectLootMismatch_WhenItemsMatch()
        {
            await using TestDatabaseContext testDb = await CreateDatabaseAsync();

            await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
            {
                db.Items.AddRange(
                    new ItemEntity
                    {
                        ContentId = 1,
                        Name = "giant shimmering pearl",
                        NormalizedName = "GIANT SHIMMERING PEARL",
                        ActualName = "Giant Shimmering Pearl",
                        Value = 0,
                        NpcValue = 3000,
                        NpcPrice = 3000,
                        ContentHash = "pearl"
                    },
                    new ItemEntity
                    {
                        ContentId = 2,
                        Name = "war axe",
                        NormalizedName = "WAR AXE",
                        ActualName = "War Axe",
                        Value = 12000,
                        NpcValue = 12000,
                        NpcPrice = 12000,
                        ContentHash = "war-axe"
                    });

                await db.SaveChangesAsync();
            }

            HuntSessionEntity session = new()
            {
                Duration = TimeSpan.FromHours(1),
                Loot = 40_000,
                LootItems =
                [
                    new HuntLootEntry { ItemName = "giant shimmering pearl", Amount = 20 },
                    new HuntLootEntry { ItemName = "war axe", Amount = 3 }
                ]
            };

            HuntSessionVerificationService service = new(testDb.Factory);
            HuntSessionVerificationResult result = await service.VerifyAsync(session);

            result.CalculatedLoot.Should().Be(96_000);
            result.LootDelta.Should().Be(56_000);
            result.HasLootMismatch.Should().BeTrue();
            result.CanApplyLootCorrection.Should().BeTrue();
            result.UnmatchedLootItems.Should().BeEmpty();
        }

        [Fact(DisplayName = "🧮 Verification: Detects raw XP mismatch when raw XP is available")]
        public async Task VerifyAsync_ShouldDetectRawXpMismatch_WhenRawXpExists()
        {
            await using TestDatabaseContext testDb = await CreateDatabaseAsync();

            await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
            {
                db.Creatures.AddRange(
                    new CreatureEntity
                    {
                        ContentId = 1,
                        Name = "falcon knight",
                        ActualName = "Falcon Knight",
                        Exp = 15_000,
                        ContentHash = "falcon-knight",
                        UpdatedAt = DateTimeOffset.UtcNow,
                        SourceLastUpdatedAt = DateTimeOffset.UtcNow
                    },
                    new CreatureEntity
                    {
                        ContentId = 2,
                        Name = "falcon paladin",
                        ActualName = "Falcon Paladin",
                        Exp = 18_000,
                        ContentHash = "falcon-paladin",
                        UpdatedAt = DateTimeOffset.UtcNow,
                        SourceLastUpdatedAt = DateTimeOffset.UtcNow
                    });

                await db.SaveChangesAsync();
            }

            HuntSessionEntity session = new()
            {
                Duration = TimeSpan.FromHours(1),
                RawXpGain = 200_000,
                XpGain = 300_000,
                XpPerHour = 300_000,
                KilledMonsters =
                [
                    new HuntMonsterEntry { MonsterName = "falcon knight", Amount = 10 },
                    new HuntMonsterEntry { MonsterName = "falcon paladin", Amount = 5 }
                ]
            };

            HuntSessionVerificationService service = new(testDb.Factory);
            HuntSessionVerificationResult result = await service.VerifyAsync(session);

            result.CalculatedRawXpGain.Should().Be(240_000);
            result.RawXpDelta.Should().Be(40_000);
            result.HasRawXpMismatch.Should().BeTrue();
            result.CalculatedXpGain.Should().Be(360_000);
            result.XpDelta.Should().Be(60_000);
            result.HasXpMismatch.Should().BeTrue();
            result.CanApplyXpCorrection.Should().BeTrue();
            result.IsXpEstimated.Should().BeFalse();
        }

        [Fact(DisplayName = "🧮 Verification: Estimates XP when raw XP is missing")]
        public async Task VerifyAsync_ShouldEstimateXp_WhenRawXpIsMissing()
        {
            await using TestDatabaseContext testDb = await CreateDatabaseAsync();

            await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
            {
                db.Creatures.Add(new CreatureEntity
                {
                    ContentId = 1,
                    Name = "falcon knight",
                    ActualName = "Falcon Knight",
                    Exp = 10_000,
                    ContentHash = "falcon-knight",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    SourceLastUpdatedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync();
            }

            HuntSessionEntity session = new()
            {
                Duration = TimeSpan.FromHours(1),
                XpGain = 100_000,
                XpPerHour = 100_000,
                CustomXpRatePercent = 150,
                KilledMonsters =
                [
                    new HuntMonsterEntry { MonsterName = "falcon knight", Amount = 10 }
                ]
            };

            HuntSessionVerificationService service = new(testDb.Factory);
            HuntSessionVerificationResult result = await service.VerifyAsync(session);

            result.ReportedRawXpGain.Should().BeNull();
            result.CalculatedRawXpGain.Should().Be(100_000);
            result.CalculatedXpGain.Should().Be(150_000);
            result.XpDelta.Should().Be(50_000);
            result.HasXpMismatch.Should().BeTrue();
            result.IsXpEstimated.Should().BeTrue();
            result.CanApplyXpCorrection.Should().BeTrue();
        }

        [Fact(DisplayName = "🧮 Verification: Ignores small deltas within threshold")]
        public async Task VerifyAsync_ShouldIgnoreSmallDeltasWithinThreshold()
        {
            await using TestDatabaseContext testDb = await CreateDatabaseAsync();

            await using (AppDbContext db = await testDb.Factory.CreateDbContextAsync())
            {
                db.Items.Add(new ItemEntity
                {
                    ContentId = 1,
                    Name = "giant shimmering pearl",
                    NormalizedName = "GIANT SHIMMERING PEARL",
                    ActualName = "Giant Shimmering Pearl",
                    Value = 0,
                    NpcValue = 3000,
                    NpcPrice = 3000,
                    ContentHash = "pearl"
                });
                db.Creatures.Add(new CreatureEntity
                {
                    ContentId = 1,
                    Name = "falcon knight",
                    ActualName = "Falcon Knight",
                    Exp = 10_000,
                    ContentHash = "falcon-knight",
                    UpdatedAt = DateTimeOffset.UtcNow,
                    SourceLastUpdatedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync();
            }

            HuntSessionEntity session = new()
            {
                Duration = TimeSpan.FromHours(1),
                Loot = 95_000,
                XpGain = 145_500,
                XpPerHour = 145_500,
                RawXpGain = 96_000,
                LootItems = [new HuntLootEntry { ItemName = "giant shimmering pearl", Amount = 30 }],
                KilledMonsters = [new HuntMonsterEntry { MonsterName = "falcon knight", Amount = 10 }]
            };

            HuntSessionVerificationService service = new(testDb.Factory);
            HuntSessionVerificationResult result = await service.VerifyAsync(session);

            result.CalculatedLoot.Should().Be(90_000);
            result.HasLootMismatch.Should().BeFalse();
            result.HasRawXpMismatch.Should().BeFalse();
            result.HasXpMismatch.Should().BeFalse();
            result.CanApplyLootCorrection.Should().BeFalse();
            result.CanApplyXpCorrection.Should().BeFalse();
        }

        [Fact(DisplayName = "🧮 Verification: Does not allow correction when lookup data is missing")]
        public async Task VerifyAsync_ShouldNotAllowCorrection_WhenLookupDataIsMissing()
        {
            await using TestDatabaseContext testDb = await CreateDatabaseAsync();

            HuntSessionEntity session = new()
            {
                Duration = TimeSpan.FromHours(1),
                Loot = 50_000,
                XpGain = 150_000,
                XpPerHour = 150_000,
                LootItems = [new HuntLootEntry { ItemName = "mystery relic", Amount = 1 }],
                KilledMonsters = [new HuntMonsterEntry { MonsterName = "unknown monster", Amount = 10 }]
            };

            HuntSessionVerificationService service = new(testDb.Factory);
            HuntSessionVerificationResult result = await service.VerifyAsync(session);

            result.HasLootMismatch.Should().BeTrue();
            result.CanApplyLootCorrection.Should().BeFalse();
            result.UnmatchedLootItems.Should().ContainSingle().Which.Should().Be("mystery relic");
            result.HasXpMismatch.Should().BeTrue();
            result.CanApplyXpCorrection.Should().BeFalse();
            result.UnmatchedCreatures.Should().ContainSingle().Which.Should().Be("unknown monster");
        }

        private static async Task<TestDatabaseContext> CreateDatabaseAsync()
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

        private sealed record TestDatabaseContext(
            SqliteConnection Connection,
            AppDbContext DbContext,
            TestDbContextFactory Factory) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await Connection.DisposeAsync();
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
