using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationExternalTaskId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalTaskId",
                table: "Nominations",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_ExternalTaskId",
                table: "Nominations",
                column: "ExternalTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nominations_ExternalTaskId",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "ExternalTaskId",
                table: "Nominations");
        }
    }
}
