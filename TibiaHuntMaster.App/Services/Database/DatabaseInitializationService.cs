using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.App.Services.Database
{
    internal readonly record struct DatabaseInitializationResult(
        bool Success,
        string ErrorMessage,
        bool UsedSchemaRepairFallback,
        string? BackupPath)
    {
        public static DatabaseInitializationResult SuccessResult(bool usedSchemaRepairFallback, string? backupPath) =>
            new(true, string.Empty, usedSchemaRepairFallback, backupPath);

        public static DatabaseInitializationResult Failure(string errorMessage, string? backupPath) =>
            new(false, errorMessage, false, backupPath);
    }

    internal sealed class DatabaseInitializationService(IServiceProvider services, Action<string>? log = null)
    {
        private readonly IServiceProvider _services = services;
        private readonly Action<string> _log = log ?? (_ => { });

        public DatabaseInitializationResult Initialize()
        {
            using IServiceScope scope = _services.CreateScope();
            IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            using AppDbContext db = factory.CreateDbContext();
            return Initialize(db, _log);
        }

        internal static DatabaseInitializationResult Initialize(AppDbContext db, Action<string>? log = null)
        {
            Action<string> writeLog = log ?? (_ => { });
            string databasePath = db.Database.GetDbConnection().DataSource;
            string? backupPath = null;

            try
            {
                DatabaseBackupResult backupResult = SqliteDatabaseBackup.TryCreatePreInitializationBackup(databasePath);
                if (backupResult.Created)
                {
                    backupPath = backupResult.BackupPath;
                    writeLog($"Created database backup at '{backupPath}' before initialization.");
                }
                else if (backupResult.Status == DatabaseBackupStatus.Failed)
                {
                    writeLog(backupResult.Message);
                }

                db.Database.Migrate();
                SqliteSchemaRepair.EnsureCriticalSchema(db);
                return DatabaseInitializationResult.SuccessResult(usedSchemaRepairFallback: false, backupPath);
            }
            catch (Exception ex)
            {
                writeLog($"Database migration failed, trying schema repair fallback: {ex}");

                try
                {
                    SqliteSchemaRepair.EnsureCriticalSchema(db);
                    writeLog("Schema repair fallback succeeded.");
                    return DatabaseInitializationResult.SuccessResult(usedSchemaRepairFallback: true, backupPath);
                }
                catch (Exception ensureEx)
                {
                    writeLog($"Failed to ensure critical schema: {ensureEx}");
                    writeLog($"Database initialization failed: {ex}");
                    return DatabaseInitializationResult.Failure(ex.Message, backupPath);
                }
            }
        }
    }
}
