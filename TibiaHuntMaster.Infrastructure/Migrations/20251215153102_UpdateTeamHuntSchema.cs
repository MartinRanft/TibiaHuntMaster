using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeamHuntSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharacterId",
                table: "TeamHuntSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SessionStartTime",
                table: "TeamHuntSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_TeamHuntSessions_CharacterId",
                table: "TeamHuntSessions",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamHuntSessions_Characters_CharacterId",
                table: "TeamHuntSessions",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamHuntSessions_Characters_CharacterId",
                table: "TeamHuntSessions");

            migrationBuilder.DropIndex(
                name: "IX_TeamHuntSessions_CharacterId",
                table: "TeamHuntSessions");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "TeamHuntSessions");

            migrationBuilder.DropColumn(
                name: "SessionStartTime",
                table: "TeamHuntSessions");
        }
    }
}
