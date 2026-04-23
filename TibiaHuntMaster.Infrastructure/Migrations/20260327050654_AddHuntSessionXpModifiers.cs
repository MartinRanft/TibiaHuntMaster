using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHuntSessionXpModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomXpRatePercent",
                table: "HuntSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XpBoostActiveMinutes",
                table: "HuntSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XpBoostPercent",
                table: "HuntSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
                                 UPDATE "HuntSessions"
                                 SET "CustomXpRatePercent" = 150
                                 WHERE "CustomXpRatePercent" IS NULL;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomXpRatePercent",
                table: "HuntSessions");

            migrationBuilder.DropColumn(
                name: "XpBoostActiveMinutes",
                table: "HuntSessions");

            migrationBuilder.DropColumn(
                name: "XpBoostPercent",
                table: "HuntSessions");
        }
    }
}
