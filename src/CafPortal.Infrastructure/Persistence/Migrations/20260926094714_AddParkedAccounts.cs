using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParkedAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkedAccounts",
                columns: table => new
                {
                    ParkedAccountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", nullable: false),
                    Tpid = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Segment = table.Column<string>(type: "TEXT", nullable: true),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    StrategicFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    PriorityWeight = table.Column<int>(type: "INTEGER", nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectManager = table.Column<string>(type: "TEXT", nullable: true),
                    SolutionArchitect = table.Column<string>(type: "TEXT", nullable: true),
                    Cftl = table.Column<string>(type: "TEXT", nullable: true),
                    AccountOwner = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerPoc = table.Column<string>(type: "TEXT", nullable: true),
                    BackupOwner = table.Column<string>(type: "TEXT", nullable: true),
                    ResourceLinks = table.Column<string>(type: "TEXT", nullable: true),
                    NominationIds = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ParkedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkedAccounts", x => x.ParkedAccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkedAccounts");
        }
    }
}
