using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Core.Hunts;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;
using TibiaHuntMaster.Infrastructure.Services.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Parsing;

namespace TibiaHuntMaster.Tests.Hunts
{
    public sealed class HuntImportConcurrencyTests
    {
        [Fact]
        public async Task ImportSessionAsync_ShouldRetry_WhenSqliteWriteIsTemporarilyLocked()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"thm-hunt-lock-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={databasePath};Default Timeout=0";

            try
            {
                await SeedCharacterAsync(connectionString, "LockTester");

                await using SqliteConnection lockConnection = new(connectionString);
                await lockConnection.OpenAsync();
                await BeginExclusiveLockAsync(lockConnection);
                Task releaseTask = ReleaseExclusiveLockAsync(lockConnection, 300);

                HuntSessionService service = CreateHuntSessionService(connectionString);
                SessionImportOptions options = new(
                    CharacterName: "LockTester",
                    RawText:
                    """
                    Session data:
                    Session: 00:10h
                    XP Gain: 225,485
                    Loot: 100,000
                    Supplies: 20,000
                    Balance: 80,000
                    """,
                    IsDoubleXp: false,
                    IsDoubleLoot: false,
                    IsRapidRespawn: false,
                    Notes: "");

                var result = await service.ImportSessionAsync(options);
                await releaseTask;

                result.Result.Should().Be(SessionImportResult.Success, result.Error);
                result.Session.Should().NotBeNull();
                result.Session!.Balance.Should().Be(80_000);
            }
            finally
            {
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }

        [Fact]
        public async Task ImportTeamSessionAsync_ShouldRetry_WhenSqliteWriteIsTemporarilyLocked()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"thm-team-lock-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={databasePath};Default Timeout=0";

            try
            {
                await SeedCharacterAsync(connectionString, "TeamLockTester");

                await using SqliteConnection lockConnection = new(connectionString);
                await lockConnection.OpenAsync();
                await BeginExclusiveLockAsync(lockConnection);
                Task releaseTask = ReleaseExclusiveLockAsync(lockConnection, 300);

                TeamHuntService service = CreateTeamHuntService(connectionString);
                const string input =
                """
                Session data:
                Session: 00:20h
                Loot Type: Leader
                Loot: 190,957
                Supplies: 11,142
                Balance: 179,815
                Player One (Leader)
                Loot: 190,957
                Supplies: 11,142
                Balance: 179,815
                """;

                var result = await service.ImportTeamSessionAsync(input, "TeamLockTester");
                await releaseTask;

                result.Result.Should().Be(SessionImportResult.Success, result.Error);
                result.Session.Should().NotBeNull();
                result.Session!.TotalBalance.Should().Be(179_815);
            }
            finally
            {
                if (File.Exists(databasePath))
                {
                    File.Delete(databasePath);
                }
            }
        }

        private static async Task SeedCharacterAsync(string connectionString, string characterName)
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                     .UseSqlite(connectionString)
                                                     .Options;

            await using AppDbContext db = new(options);
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            db.Characters.Add(new CharacterEntity
            {
                Name = characterName,
                World = "Antica",
                Vocation = "Knight",
                LastUpdatedUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        private static HuntSessionService CreateHuntSessionService(string connectionString)
        {
            return new HuntSessionService(
                new FileDbContextFactory(connectionString),
                new HuntAnalyzerParser(NullLogger<HuntAnalyzerParser>.Instance),
                NullLogger<HuntSessionService>.Instance);
        }

        private static TeamHuntService CreateTeamHuntService(string connectionString)
        {
            return new TeamHuntService(
                new FileDbContextFactory(connectionString),
                new TeamHuntParser(NullLogger<TeamHuntParser>.Instance),
                NullLogger<TeamHuntService>.Instance);
        }

        private static async Task BeginExclusiveLockAsync(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "BEGIN EXCLUSIVE;";
            await command.ExecuteNonQueryAsync();
        }

        private static async Task ReleaseExclusiveLockAsync(SqliteConnection connection, int delayMs)
        {
            await Task.Delay(delayMs);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "ROLLBACK;";
            await command.ExecuteNonQueryAsync();
        }

        private sealed class FileDbContextFactory(string connectionString) : IDbContextFactory<AppDbContext>
        {
            public AppDbContext CreateDbContext()
            {
                DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                                                         .UseSqlite(connectionString)
                                                         .Options;
                return new AppDbContext(options);
            }

            public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
            {
                return Task.FromResult(CreateDbContext());
            }
        }
    }
}
