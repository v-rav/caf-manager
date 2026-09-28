using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Master list of migration products/tools (GHCP, AppMod, Accelerators, Azure tooling…). Portal-owned, editable.</summary>
public class MigrationTool : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Grouping: Assessment · GHCP · AppMod · Accelerator · IaC · Other.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Who ships it (Microsoft, GitHub, Partner…).</summary>
    public string? Vendor { get; set; }
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
