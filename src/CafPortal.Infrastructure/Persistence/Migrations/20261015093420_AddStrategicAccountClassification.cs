using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategicAccountClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Target of the default SegmentClass rule (FDO Strategic segment → "Strategic Account"); added once if missing.
            migrationBuilder.Sql(@"
INSERT INTO LookupValues (Category, Value, SortOrder, ActiveFlag, CreatedUtc)
SELECT 'Classification', 'Strategic Account',
       COALESCE((SELECT MAX(SortOrder) FROM LookupValues WHERE Category = 'Classification'), 0) + 1, 1, strftime('%Y-%m-%d %H:%M:%S+00:00', 'now')
WHERE NOT EXISTS (SELECT 1 FROM LookupValues WHERE Category = 'Classification' AND Value = 'Strategic Account');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM LookupValues WHERE Category = 'Classification' AND Value = 'Strategic Account';");
        }
    }
}
