using System.Data.Common;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaHuntMaster.App.Services.Database;
using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class DatabaseInitializationServiceTests
    {
        [Fact]
        public void Initialize_ShouldRecoverLegacyFileDatabase_AndCreateBackup()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-db-init-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string databasePath = Path.Combine(tempDir, "tibiahuntmaster.db");

            try
            {
                CreateLegacyDatabase(databasePath);

                using ServiceProvider provider = CreateServiceProvider(databasePath);
                DatabaseInitializationService initializer = new(provider);

                DatabaseInitializationResult result = initializer.Initialize();

                result.Success.Should().BeTrue(result.ErrorMessage);
                result.UsedSchemaRepairFallback.Should().BeTrue();
                result.BackupPath.Should().NotBeNullOrWhiteSpace();
                File.Exists(result.BackupPath!).Should().BeTrue();
                Directory.GetFiles(tempDir, "tibiahuntmaster.db.preinit-*.bak").Should().HaveCount(1);

                using SqliteConnection connection = new($"Data Source={databasePath};Mode=ReadOnly");
                connection.Open();

                TableHasColumn(connection, "HuntSessions", "HuntingPlaceId").Should().BeTrue();
                TableHasColumn(connection, "HuntSessions", "CustomXpRatePercent").Should().BeTrue();
                TableHasColumn(connection, "TeamHuntSessions", "XpGain").Should().BeTrue();
                TableHasColumn(connection, "TeamHuntSessions", "XpPerHour").Should().BeTrue();
                TableHasColumn(connection, "TeamHuntSessions", "Notes").Should().BeTrue();

                TableExists(connection, "HuntGoalConnections").Should().BeTrue();
                TableExists(connection, "CharacterDepotSales").Should().BeTrue();
                TableExists(connection, "MonsterSpawnCoordinates").Should().BeTrue();
                TableExists(connection, "MonsterSpawnCreatureLinks").Should().BeTrue();
                TableExists(connection, "MonsterImageAssets").Should().BeTrue();
                TableExists(connection, "MonsterImageAliases").Should().BeTrue();
                TableExists(connection, "CreatureMonsterImageLinks").Should().BeTrue();

                ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM HuntSessions;").Should().Be(1);
                ExecuteScalar<long>(connection, "SELECT COUNT(*) FROM TeamHuntSessions;").Should().Be(1);
                ExecuteScalar<long>(connection, "SELECT \"CustomXpRatePercent\" FROM HuntSessions WHERE Id = 1;").Should().Be(150);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        private static ServiceProvider CreateServiceProvider(string databasePath)
        {
            ServiceCollection services = new();
            services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
            return services.BuildServiceProvider();
        }

        private static void CreateLegacyDatabase(string databasePath)
        {
            using SqliteConnection connection = new($"Data Source={databasePath}");
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            """
            PRAGMA foreign_keys = OFF;

            CREATE TABLE Characters (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );

            CREATE TABLE CharacterGoals (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CharacterId INTEGER NOT NULL
            );

            CREATE TABLE Creatures (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Name TEXT NULL
            );

            CREATE TABLE HuntSessions (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CharacterId INTEGER NOT NULL,
                SessionStartTime TEXT NULL
            );

            CREATE TABLE TeamHuntSessions (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                CharacterId INTEGER NOT NULL,
                SessionStartTime TEXT NULL
            );

            INSERT INTO Characters (Id, Name) VALUES (1, 'LegacyChar');
            INSERT INTO CharacterGoals (Id, CharacterId) VALUES (1, 1);
            INSERT INTO Creatures (Id, Name) VALUES (1, 'Dragon');
            INSERT INTO HuntSessions (Id, CharacterId, SessionStartTime) VALUES (1, 1, '2026-03-01T10:00:00Z');
            INSERT INTO TeamHuntSessions (Id, CharacterId, SessionStartTime) VALUES (1, 1, '2026-03-01T11:00:00Z');
            """;
            command.ExecuteNonQuery();
        }

        private static bool TableExists(DbConnection connection, string tableName)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name = $name;";
            SqliteParameter parameter = command.CreateParameter() as SqliteParameter ?? new SqliteParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        private static bool TableHasColumn(DbConnection connection, string tableName, string columnName)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static T ExecuteScalar<T>(DbConnection connection, string sql)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            return (T)Convert.ChangeType(command.ExecuteScalar(), typeof(T))!;
        }
    }
}
