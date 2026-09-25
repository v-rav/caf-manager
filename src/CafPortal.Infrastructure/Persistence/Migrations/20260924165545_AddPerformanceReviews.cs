using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PersonName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReportingManager = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CommunicationVerbal = table.Column<double>(type: "REAL", nullable: true),
                    CommunicationWritten = table.Column<double>(type: "REAL", nullable: true),
                    Attitude = table.Column<double>(type: "REAL", nullable: true),
                    ProcessUnderstanding = table.Column<double>(type: "REAL", nullable: true),
                    OfferingUnderstanding = table.Column<double>(type: "REAL", nullable: true),
                    Comments = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_PersonName_ReviewDate",
                table: "PerformanceReviews",
                columns: new[] { "PersonName", "ReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_Region",
                table: "PerformanceReviews",
                column: "Region");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceReviews");
        }
    }
}
