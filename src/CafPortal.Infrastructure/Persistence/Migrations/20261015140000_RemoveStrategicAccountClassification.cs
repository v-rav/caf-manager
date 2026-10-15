using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(AppDbContext))]
    [Migration("20261015140000_RemoveStrategicAccountClassification")]
    public partial class RemoveStrategicAccountClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Segment is not an engagement classification: drop the value, its segment rule, and any stored use of it.
            migrationBuilder.Sql("DELETE FROM LookupValues WHERE Category = 'Classification' AND Value = 'Strategic Account';");
            migrationBuilder.Sql("DELETE FROM KeywordRules WHERE Target = 'Strategic Account';");
            migrationBuilder.Sql("UPDATE Nominations SET Classification = NULL WHERE Classification = 'Strategic Account';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
