using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernanceGates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GateDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ExitCriteria = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerRole = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GateItemDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GateDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    SubStage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ResponsibleRole = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Mandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateItemDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateItemDefinitions_GateDefinitions_GateDefinitionId",
                        column: x => x.GateDefinitionId,
                        principalTable: "GateDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NominationGateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NominationId = table.Column<int>(type: "INTEGER", nullable: false),
                    GateItemDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Ref = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationGateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NominationGateItems_GateItemDefinitions_GateItemDefinitionId",
                        column: x => x.GateItemDefinitionId,
                        principalTable: "GateItemDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GateDefinitions_Key",
                table: "GateDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GateItemDefinitions_GateDefinitionId_Key",
                table: "GateItemDefinitions",
                columns: new[] { "GateDefinitionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NominationGateItems_GateItemDefinitionId",
                table: "NominationGateItems",
                column: "GateItemDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationGateItems_NominationId_GateItemDefinitionId",
                table: "NominationGateItems",
                columns: new[] { "NominationId", "GateItemDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NominationGateItems");

            migrationBuilder.DropTable(
                name: "GateItemDefinitions");

            migrationBuilder.DropTable(
                name: "GateDefinitions");
        }
    }
}
