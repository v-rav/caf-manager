using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFdoStageDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FdoStage1Days",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FdoStage2Days",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FdoStage3Days",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FdoStage4Days",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FdoTotalDays",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FdoStage1Days",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "FdoStage2Days",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "FdoStage3Days",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "FdoStage4Days",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "FdoTotalDays",
                table: "Nominations");
        }
    }
}
