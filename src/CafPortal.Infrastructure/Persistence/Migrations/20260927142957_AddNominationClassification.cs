using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VelocityImpact",
                table: "Nominations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classification",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "VelocityImpact",
                table: "Nominations");
        }
    }
}
