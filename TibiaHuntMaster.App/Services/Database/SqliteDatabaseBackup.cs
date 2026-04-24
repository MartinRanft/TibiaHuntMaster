using System.Globalization;

using Microsoft.Data.Sqlite;

namespace TibiaHuntMaster.App.Services.Database
{
    internal enum DatabaseBackupStatus
    {
        Created,
        Skipped,
        Failed
    }

    internal readonly record struct DatabaseBackupResult(
        DatabaseBackupStatus Status,
        string? BackupPath,
        string Message)
    {
        public bool Created => Status == DatabaseBackupStatus.Created;

        public static DatabaseBackupResult CreatedResult(string backupPath) =>
            new(DatabaseBackupStatus.Created, backupPath, "Backup created");

        public static DatabaseBackupResult Skipped(string message) =>
            new(DatabaseBackupStatus.Skipped, null, message);

        public static DatabaseBackupResult Failed(string message) =>
            new(DatabaseBackupStatus.Failed, null, message);
    }

    internal static class SqliteDatabaseBackup
    {
        private const int DefaultMaxBackupFiles = 3;
        private static int _backupSequence;

        public static DatabaseBackupResult TryCreatePreInitializationBackup(string? databasePath, int maxBackupFiles = DefaultMaxBackupFiles)
        {
            if (maxBackupFiles < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBackupFiles));
            }

            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return DatabaseBackupResult.Skipped("Database path is empty.");
            }

            if (string.Equals(databasePath, ":memory:", StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseBackupResult.Skipped("In-memory SQLite database does not need a backup.");
            }

            string fullPath = Path.GetFullPath(databasePath);
            if (!File.Exists(fullPath))
            {
                return DatabaseBackupResult.Skipped("Database file does not exist yet.");
            }

            FileInfo fileInfo = new(fullPath);
            if (fileInfo.Length == 0)
            {
                return DatabaseBackupResult.Skipped("Database file is empty.");
            }

            string backupPath = BuildBackupPath(fileInfo);

            try
            {
                using SqliteConnection source = new($"Data Source={fullPath};Mode=ReadOnly");
                using SqliteConnection destination = new($"Data Source={backupPath}");
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);

                TrimOldBackups(fileInfo.DirectoryName!, fileInfo.Name, maxBackupFiles);
                return DatabaseBackupResult.CreatedResult(backupPath);
            }
            catch (Exception ex)
            {
                return DatabaseBackupResult.Failed($"Failed to create SQLite backup: {ex.Message}");
            }
        }

        private static string BuildBackupPath(FileInfo sourceFile)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            int sequence = Interlocked.Increment(ref _backupSequence);
            string backupFileName = $"{sourceFile.Name}.preinit-{timestamp}-{sequence:D4}.bak";
            return Path.Combine(sourceFile.DirectoryName!, backupFileName);
        }

        private static void TrimOldBackups(string directory, string databaseFileName, int maxBackupFiles)
        {
            string searchPattern = $"{databaseFileName}.preinit-*.bak";

            string[] backupFiles = Directory
                .EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .Skip(maxBackupFiles)
                .ToArray();

            foreach (string backupFile in backupFiles)
            {
                File.Delete(backupFile);
            }
        }
    }
}
