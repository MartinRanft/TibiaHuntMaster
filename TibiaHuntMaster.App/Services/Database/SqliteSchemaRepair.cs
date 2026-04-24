using System.Data;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using TibiaHuntMaster.Infrastructure.Data;

namespace TibiaHuntMaster.App.Services.Database
{
    internal static class SqliteSchemaRepair
    {
        public static void EnsureCriticalSchema(AppDbContext db)
        {
            if (!db.Database.IsSqlite())
            {
                return;
            }

            EnsureHuntGoalConnectionsTable(db);
            EnsureDepotSalesTable(db);
            EnsureTeamHuntSessionColumns(db);
            EnsureHuntSessionColumns(db);
            EnsureMonsterSpawnTables(db);
            EnsureMonsterImageTables(db);
        }

        private static void EnsureHuntGoalConnectionsTable(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "HuntGoalConnections" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_HuntGoalConnections" PRIMARY KEY AUTOINCREMENT,
                    "CharacterGoalId" INTEGER NOT NULL,
                    "HuntSessionId" INTEGER NULL,
                    "TeamHuntSessionId" INTEGER NULL,
                    "IsFinisher" INTEGER NOT NULL,
                    CONSTRAINT "FK_HuntGoalConnections_CharacterGoals_CharacterGoalId"
                        FOREIGN KEY ("CharacterGoalId") REFERENCES "CharacterGoals" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_HuntGoalConnections_HuntSessions_HuntSessionId"
                        FOREIGN KEY ("HuntSessionId") REFERENCES "HuntSessions" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_HuntGoalConnections_TeamHuntSessions_TeamHuntSessionId"
                        FOREIGN KEY ("TeamHuntSessionId") REFERENCES "TeamHuntSessions" ("Id") ON DELETE CASCADE
                );
                """);

            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_CharacterGoalId\" ON \"HuntGoalConnections\" (\"CharacterGoalId\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_HuntSessionId\" ON \"HuntGoalConnections\" (\"HuntSessionId\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_TeamHuntSessionId\" ON \"HuntGoalConnections\" (\"TeamHuntSessionId\");");
        }

        private static void EnsureDepotSalesTable(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "CharacterDepotSales" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CharacterDepotSales" PRIMARY KEY AUTOINCREMENT,
                    "CharacterId" INTEGER NOT NULL,
                    "SoldAtUtc" INTEGER NOT NULL,
                    "RealizedValue" INTEGER NOT NULL,
                    "CreatedAtUtc" INTEGER NOT NULL,
                    CONSTRAINT "FK_CharacterDepotSales_Characters_CharacterId"
                        FOREIGN KEY ("CharacterId") REFERENCES "Characters" ("Id") ON DELETE CASCADE
                );
                """);

            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_CharacterDepotSales_CharacterId_SoldAtUtc\" ON \"CharacterDepotSales\" (\"CharacterId\", \"SoldAtUtc\");");
        }

        private static void EnsureTeamHuntSessionColumns(AppDbContext db)
        {
            HashSet<string> columns = GetSqliteTableColumns(db, "TeamHuntSessions");

            if (columns.Count == 0)
            {
                return;
            }

            if (!columns.Contains("XpGain"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"TeamHuntSessions\" ADD COLUMN \"XpGain\" INTEGER NOT NULL DEFAULT 0;");
            }

            if (!columns.Contains("XpPerHour"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"TeamHuntSessions\" ADD COLUMN \"XpPerHour\" INTEGER NOT NULL DEFAULT 0;");
            }

            if (!columns.Contains("Notes"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"TeamHuntSessions\" ADD COLUMN \"Notes\" TEXT NULL;");
            }
        }

        private static void EnsureHuntSessionColumns(AppDbContext db)
        {
            HashSet<string> columns = GetSqliteTableColumns(db, "HuntSessions");

            if (columns.Count == 0)
            {
                return;
            }

            if (!columns.Contains("HuntingPlaceId"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"HuntingPlaceId\" INTEGER NULL;");
            }

            if (!columns.Contains("XpBoostPercent"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"XpBoostPercent\" INTEGER NULL;");
            }

            if (!columns.Contains("XpBoostActiveMinutes"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"XpBoostActiveMinutes\" INTEGER NULL;");
            }

            if (!columns.Contains("CustomXpRatePercent"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"CustomXpRatePercent\" INTEGER NULL;");
            }

            if (!columns.Contains("RawXpGain"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"RawXpGain\" INTEGER NULL;");
            }

            if (!columns.Contains("IgnoreLootVerificationWarning"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"IgnoreLootVerificationWarning\" INTEGER NOT NULL DEFAULT 0;");
            }

            if (!columns.Contains("IgnoreXpVerificationWarning"))
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE \"HuntSessions\" ADD COLUMN \"IgnoreXpVerificationWarning\" INTEGER NOT NULL DEFAULT 0;");
            }

            db.Database.ExecuteSqlRaw("UPDATE \"HuntSessions\" SET \"CustomXpRatePercent\" = 150 WHERE \"CustomXpRatePercent\" IS NULL;");

            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_HuntSessions_HuntingPlaceId\" ON \"HuntSessions\" (\"HuntingPlaceId\");");
        }

        private static void EnsureMonsterSpawnTables(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "MonsterSpawnCoordinates" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_MonsterSpawnCoordinates" PRIMARY KEY AUTOINCREMENT,
                    "X" INTEGER NOT NULL,
                    "Y" INTEGER NOT NULL,
                    "Z" INTEGER NOT NULL
                );
                """);

            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "MonsterSpawnCreatureLinks" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_MonsterSpawnCreatureLinks" PRIMARY KEY AUTOINCREMENT,
                    "MonsterSpawnCoordinateId" INTEGER NOT NULL,
                    "CreatureId" INTEGER NULL,
                    "MonsterName" TEXT NOT NULL,
                    "SpawnTimeSeconds" INTEGER NULL,
                    "Direction" INTEGER NULL,
                    CONSTRAINT "FK_MonsterSpawnCreatureLinks_MonsterSpawnCoordinates_MonsterSpawnCoordinateId"
                        FOREIGN KEY ("MonsterSpawnCoordinateId") REFERENCES "MonsterSpawnCoordinates" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId"
                        FOREIGN KEY ("CreatureId") REFERENCES "Creatures" ("Id") ON DELETE SET NULL
                );
                """);

            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_MonsterSpawnCoordinates_X_Y_Z\" ON \"MonsterSpawnCoordinates\" (\"X\", \"Y\", \"Z\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterSpawnCoordinates_Z_X_Y\" ON \"MonsterSpawnCoordinates\" (\"Z\", \"X\", \"Y\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterSpawnCreatureLinks_MonsterSpawnCoordinateId\" ON \"MonsterSpawnCreatureLinks\" (\"MonsterSpawnCoordinateId\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterSpawnCreatureLinks_CreatureId\" ON \"MonsterSpawnCreatureLinks\" (\"CreatureId\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterSpawnCreatureLinks_MonsterName\" ON \"MonsterSpawnCreatureLinks\" (\"MonsterName\");");
        }

        private static void EnsureMonsterImageTables(AppDbContext db)
        {
            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "MonsterImageAssets" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_MonsterImageAssets" PRIMARY KEY AUTOINCREMENT,
                    "CanonicalSlug" TEXT NOT NULL,
                    "AssetUri" TEXT NOT NULL,
                    "ContentHash" TEXT NOT NULL,
                    "FileSizeBytes" INTEGER NOT NULL
                );
                """);

            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "MonsterImageAliases" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_MonsterImageAliases" PRIMARY KEY AUTOINCREMENT,
                    "Slug" TEXT NOT NULL,
                    "MonsterImageAssetId" INTEGER NOT NULL,
                    CONSTRAINT "FK_MonsterImageAliases_MonsterImageAssets_MonsterImageAssetId"
                        FOREIGN KEY ("MonsterImageAssetId") REFERENCES "MonsterImageAssets" ("Id") ON DELETE CASCADE
                );
                """);

            db.Database.ExecuteSqlRaw(
                """
                CREATE TABLE IF NOT EXISTS "CreatureMonsterImageLinks" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_CreatureMonsterImageLinks" PRIMARY KEY AUTOINCREMENT,
                    "CreatureId" INTEGER NOT NULL,
                    "MonsterImageAssetId" INTEGER NOT NULL,
                    "MatchedBySlug" TEXT NULL,
                    CONSTRAINT "FK_CreatureMonsterImageLinks_Creatures_CreatureId"
                        FOREIGN KEY ("CreatureId") REFERENCES "Creatures" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_CreatureMonsterImageLinks_MonsterImageAssets_MonsterImageAssetId"
                        FOREIGN KEY ("MonsterImageAssetId") REFERENCES "MonsterImageAssets" ("Id") ON DELETE CASCADE
                );
                """);

            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_MonsterImageAssets_ContentHash\" ON \"MonsterImageAssets\" (\"ContentHash\");");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_MonsterImageAssets_AssetUri\" ON \"MonsterImageAssets\" (\"AssetUri\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterImageAssets_CanonicalSlug\" ON \"MonsterImageAssets\" (\"CanonicalSlug\");");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_MonsterImageAliases_Slug\" ON \"MonsterImageAliases\" (\"Slug\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_MonsterImageAliases_MonsterImageAssetId\" ON \"MonsterImageAliases\" (\"MonsterImageAssetId\");");
            db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CreatureMonsterImageLinks_CreatureId\" ON \"CreatureMonsterImageLinks\" (\"CreatureId\");");
            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_CreatureMonsterImageLinks_MonsterImageAssetId\" ON \"CreatureMonsterImageLinks\" (\"MonsterImageAssetId\");");
        }

        private static HashSet<string> GetSqliteTableColumns(AppDbContext db, string tableName)
        {
            HashSet<string> columns = new(StringComparer.OrdinalIgnoreCase);

            DbConnection connection = db.Database.GetDbConnection();
            bool shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                connection.Open();
            }

            try
            {
                using DbCommand command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

                using DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetValue(1) is string name && !string.IsNullOrWhiteSpace(name))
                    {
                        columns.Add(name);
                    }
                }
            }
            finally
            {
                if (shouldClose)
                {
                    connection.Close();
                }
            }

            return columns;
        }
    }
}
