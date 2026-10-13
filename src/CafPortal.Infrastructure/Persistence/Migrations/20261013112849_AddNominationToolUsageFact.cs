using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationToolUsageFact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NominationToolUsageFacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalAccountId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Tpid = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    TaskId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ToolName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationToolUsageFacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NominationToolUsageFacts_Nominations_NominationId",
                        column: x => x.NominationId,
                        principalTable: "Nominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NominationToolUsageFacts_NominationId",
                table: "NominationToolUsageFacts",
                column: "NominationId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationToolUsageFacts_TaskId",
                table: "NominationToolUsageFacts",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NominationToolUsageFacts");
        }
    }
}
