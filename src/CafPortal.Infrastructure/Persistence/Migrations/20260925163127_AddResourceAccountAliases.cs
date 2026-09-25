using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceAccountAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_Email",
                table: "Resources");

            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "Resources",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "Accounts",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Email",
                table: "Resources",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [Email] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_Email",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Email",
                table: "Resources",
                column: "Email");
        }
    }
}
