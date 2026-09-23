using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountMasterKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                table: "Accounts",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tpid",
                table: "Accounts",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Tpid",
                table: "Accounts",
                column: "Tpid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_Tpid",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Tpid",
                table: "Accounts");
        }
    }
}
