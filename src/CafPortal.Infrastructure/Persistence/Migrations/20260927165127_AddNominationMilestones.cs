using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NominationMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: false),
                    MilestoneKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ToolUsed = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationMilestones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NominationMilestones_NominationId_OccurredOn",
                table: "NominationMilestones",
                columns: new[] { "NominationId", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NominationMilestones");

            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "Nominations");
        }
    }
}
