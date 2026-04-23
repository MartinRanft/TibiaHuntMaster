using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterSpawnTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonsterSpawnCoordinates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Z = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterSpawnCoordinates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonsterSpawnCreatureLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MonsterSpawnCoordinateId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: true),
                    MonsterName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SpawnTimeSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    Direction = table.Column<byte>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterSpawnCreatureLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonsterSpawnCreatureLinks_MonsterSpawnCoordinates_MonsterSpawnCoordinateId",
                        column: x => x.MonsterSpawnCoordinateId,
                        principalTable: "MonsterSpawnCoordinates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCoordinates_X_Y_Z",
                table: "MonsterSpawnCoordinates",
                columns: new[] { "X", "Y", "Z" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCoordinates_Z_X_Y",
                table: "MonsterSpawnCoordinates",
                columns: new[] { "Z", "X", "Y" });

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCreatureLinks_CreatureId",
                table: "MonsterSpawnCreatureLinks",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCreatureLinks_MonsterSpawnCoordinateId",
                table: "MonsterSpawnCreatureLinks",
                column: "MonsterSpawnCoordinateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonsterSpawnCreatureLinks");

            migrationBuilder.DropTable(
                name: "MonsterSpawnCoordinates");
        }
    }
}
