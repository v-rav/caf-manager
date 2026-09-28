using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationCapability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigrationActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MigrationTools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Vendor = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationTools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NominationToolUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToolId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: true),
                    UsedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UsedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationToolUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigrationActivities_Name",
                table: "MigrationActivities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MigrationTools_Name",
                table: "MigrationTools",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NominationToolUsages_ActivityId",
                table: "NominationToolUsages",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationToolUsages_NominationId",
                table: "NominationToolUsages",
                column: "NominationId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationToolUsages_ToolId",
                table: "NominationToolUsages",
                column: "ToolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigrationActivities");

            migrationBuilder.DropTable(
                name: "MigrationTools");

            migrationBuilder.DropTable(
                name: "NominationToolUsages");
        }
    }
}
