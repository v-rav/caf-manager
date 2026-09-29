using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAcrRecoveryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcrRecoveryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: false),
                    GapType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    BaselineCores = table.Column<int>(type: "INTEGER", nullable: true),
                    BaselineAcr = table.Column<decimal>(type: "TEXT", nullable: true),
                    RecommendedCores = table.Column<int>(type: "INTEGER", nullable: true),
                    RecommendedAcr = table.Column<decimal>(type: "TEXT", nullable: true),
                    FlaggedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FlaggedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    NotifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NotifiedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    RealizedCores = table.Column<int>(type: "INTEGER", nullable: true),
                    RealizedAcr = table.Column<decimal>(type: "TEXT", nullable: true),
                    RealizedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecoveredAcr = table.Column<decimal>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClosedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcrRecoveryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcrRecoveryEntries_Nominations_NominationId",
                        column: x => x.NominationId,
                        principalTable: "Nominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcrRecoveryEntries_NominationId_Status",
                table: "AcrRecoveryEntries",
                columns: new[] { "NominationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcrRecoveryEntries");
        }
    }
}
