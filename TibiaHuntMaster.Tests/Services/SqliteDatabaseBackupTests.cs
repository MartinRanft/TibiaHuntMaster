using FluentAssertions;
using Microsoft.Data.Sqlite;

using TibiaHuntMaster.App.Services.Database;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class SqliteDatabaseBackupTests
    {
        [Fact]
        public void TryCreatePreInitializationBackup_ShouldCreateValidBackup_AndKeepOnlyNewestThree()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-db-backup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string databasePath = Path.Combine(tempDir, "tibiahuntmaster.db");

            try
            {
                CreateSeedDatabase(databasePath);

                for (int i = 0; i < 5; i++)
                {
                    DatabaseBackupResult result = SqliteDatabaseBackup.TryCreatePreInitializationBackup(databasePath);
                    result.Status.Should().Be(DatabaseBackupStatus.Created);
                    result.BackupPath.Should().NotBeNullOrWhiteSpace();
                    File.Exists(result.BackupPath!).Should().BeTrue();
                }

                string[] backupFiles = Directory.GetFiles(tempDir, "tibiahuntmaster.db.preinit-*.bak");
                backupFiles.Should().HaveCount(3);

                string newestBackup = backupFiles.OrderByDescending(Path.GetFileName, StringComparer.Ordinal).First();
                using SqliteConnection connection = new($"Data Source={newestBackup};Mode=ReadOnly");
                connection.Open();

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM SampleData;";
                long rowCount = (long)(command.ExecuteScalar() ?? 0L);

                rowCount.Should().Be(1);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void TryCreatePreInitializationBackup_ShouldSkip_WhenDatabaseFileDoesNotExist()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "thm-db-backup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string databasePath = Path.Combine(tempDir, "missing.db");

            try
            {
                DatabaseBackupResult result = SqliteDatabaseBackup.TryCreatePreInitializationBackup(databasePath);

                result.Status.Should().Be(DatabaseBackupStatus.Skipped);
                result.Message.Should().Contain("does not exist");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        private static void CreateSeedDatabase(string databasePath)
        {
            using SqliteConnection connection = new($"Data Source={databasePath}");
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
            """
            CREATE TABLE SampleData (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );

            INSERT INTO SampleData (Name) VALUES ('seed');
            """;
            command.ExecuteNonQuery();
        }
    }
}
