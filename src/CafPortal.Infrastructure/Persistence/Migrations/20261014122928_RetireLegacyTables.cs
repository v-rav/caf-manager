using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "StrategicAccountConfigurations");

            migrationBuilder.DropTable(
                name: "Tools");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Granted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermissionKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleConfigurationId",
                        column: x => x.RoleConfigurationId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicAccountConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExecutiveVisibilityFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    PriorityWeight = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    StrategicFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicAccountConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicAccountConfigurations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActiveFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleConfigurationId_PermissionKey",
                table: "RolePermissions",
                columns: new[] { "RoleConfigurationId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicAccountConfigurations_AccountId",
                table: "StrategicAccountConfigurations",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicAccountConfigurations_AccountName",
                table: "StrategicAccountConfigurations",
                column: "AccountName");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Name",
                table: "Tools",
                column: "Name",
                unique: true);
        }
    }
}
