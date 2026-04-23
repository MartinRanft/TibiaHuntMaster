using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterImageCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonsterImageAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalSlug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AssetUri = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterImageAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreatureMonsterImageLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    MonsterImageAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchedBySlug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatureMonsterImageLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatureMonsterImageLinks_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreatureMonsterImageLinks_MonsterImageAssets_MonsterImageAssetId",
                        column: x => x.MonsterImageAssetId,
                        principalTable: "MonsterImageAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonsterImageAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MonsterImageAssetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonsterImageAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonsterImageAliases_MonsterImageAssets_MonsterImageAssetId",
                        column: x => x.MonsterImageAssetId,
                        principalTable: "MonsterImageAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreatureMonsterImageLinks_CreatureId",
                table: "CreatureMonsterImageLinks",
                column: "CreatureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatureMonsterImageLinks_MonsterImageAssetId",
                table: "CreatureMonsterImageLinks",
                column: "MonsterImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAliases_MonsterImageAssetId",
                table: "MonsterImageAliases",
                column: "MonsterImageAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAliases_Slug",
                table: "MonsterImageAliases",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_AssetUri",
                table: "MonsterImageAssets",
                column: "AssetUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_CanonicalSlug",
                table: "MonsterImageAssets",
                column: "CanonicalSlug");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_ContentHash",
                table: "MonsterImageAssets",
                column: "ContentHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreatureMonsterImageLinks");

            migrationBuilder.DropTable(
                name: "MonsterImageAliases");

            migrationBuilder.DropTable(
                name: "MonsterImageAssets");
        }
    }
}
