using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKeywordRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeywordRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MatchType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Target = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordRules_Domain_Priority",
                table: "KeywordRules",
                columns: new[] { "Domain", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeywordRules");
        }
    }
}
