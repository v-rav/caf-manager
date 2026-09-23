using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CftlPrimary",
                table: "Nominations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentState",
                table: "Nominations",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MigrationStatus",
                table: "Nominations",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCoordinator",
                table: "Nominations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolutionArchitect",
                table: "Nominations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CftlPrimary",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "CurrentState",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "MigrationStatus",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "ProjectCoordinator",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "SolutionArchitect",
                table: "Nominations");
        }
    }
}
