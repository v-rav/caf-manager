using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportColumnMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Candidates = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportColumnMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DetectOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportColumnMaps_Kind_Field",
                table: "ImportColumnMaps",
                columns: new[] { "Kind", "Field" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportProfiles_Kind",
                table: "ImportProfiles",
                column: "Kind",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportColumnMaps");

            migrationBuilder.DropTable(
                name: "ImportProfiles");
        }
    }
}
