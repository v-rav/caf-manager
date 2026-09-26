using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationOfferingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutomationUsed",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsToolAttached",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerName",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryMigrationPath",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalCores",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutomationUsed",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "IsToolAttached",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "PartnerName",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "PrimaryMigrationPath",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "TotalCores",
                table: "Nominations");
        }
    }
}
