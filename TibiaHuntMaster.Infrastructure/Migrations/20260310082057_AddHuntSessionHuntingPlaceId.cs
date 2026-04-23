using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHuntSessionHuntingPlaceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HuntingPlaceId",
                table: "HuntSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HuntSessions_HuntingPlaceId",
                table: "HuntSessions",
                column: "HuntingPlaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HuntSessions_HuntingPlaceId",
                table: "HuntSessions");

            migrationBuilder.DropColumn(
                name: "HuntingPlaceId",
                table: "HuntSessions");
        }
    }
}
