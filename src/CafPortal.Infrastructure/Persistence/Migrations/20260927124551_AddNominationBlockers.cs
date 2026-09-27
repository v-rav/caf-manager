using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationBlockers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NominationBlockers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: false),
                    GateItemDefinitionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ClockStopped = table.Column<bool>(type: "INTEGER", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    BlockedSinceUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpectedResolutionUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RaisedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationBlockers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NominationBlockers_NominationId_ResolvedUtc",
                table: "NominationBlockers",
                columns: new[] { "NominationId", "ResolvedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NominationBlockers");
        }
    }
}
