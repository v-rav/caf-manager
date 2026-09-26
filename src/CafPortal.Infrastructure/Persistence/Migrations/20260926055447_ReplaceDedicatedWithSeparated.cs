using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDedicatedWithSeparated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old flag (do not carry its values over) and add a fresh Separated column.
            migrationBuilder.DropColumn(
                name: "DedicatedFlag",
                table: "Resources");

            migrationBuilder.AddColumn<bool>(
                name: "Separated",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Separated",
                table: "Resources");

            migrationBuilder.AddColumn<bool>(
                name: "DedicatedFlag",
                table: "Resources",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
