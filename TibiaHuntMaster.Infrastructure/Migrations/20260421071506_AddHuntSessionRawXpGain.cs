using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHuntSessionRawXpGain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RawXpGain",
                table: "HuntSessions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawXpGain",
                table: "HuntSessions");
        }
    }
}
