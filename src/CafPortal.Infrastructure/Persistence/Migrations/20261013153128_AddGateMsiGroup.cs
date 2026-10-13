using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGateMsiGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MsiGroup",
                table: "GateDefinitions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Readiness");

            // Carry today's key-based mapping into data so existing gates keep their MSI component.
            migrationBuilder.Sql("""
                UPDATE GateDefinitions SET MsiGroup = CASE
                    WHEN Key IN ('G1','G2','G3') THEN 'Readiness'
                    WHEN Key IN ('G4','G5') THEN 'Scope'
                    WHEN Key IN ('G6','G7') THEN 'Delivery'
                    WHEN Key = 'G8' THEN 'Signoff'
                    ELSE 'Readiness' END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MsiGroup",
                table: "GateDefinitions");
        }
    }
}
