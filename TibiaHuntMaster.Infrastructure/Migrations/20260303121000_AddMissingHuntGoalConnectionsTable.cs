using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using TibiaHuntMaster.Infrastructure.Data;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260303121000_AddMissingHuntGoalConnectionsTable")]
    public partial class AddMissingHuntGoalConnectionsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
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

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_CharacterGoalId\" ON \"HuntGoalConnections\" (\"CharacterGoalId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_HuntSessionId\" ON \"HuntGoalConnections\" (\"HuntSessionId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_HuntGoalConnections_TeamHuntSessionId\" ON \"HuntGoalConnections\" (\"TeamHuntSessionId\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"HuntGoalConnections\";");
        }
    }
}
