using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Master list of migration activities a tool can accelerate (assessment, modernization, containerization…).</summary>
public class MigrationActivity : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Journey stage grouping: Assess · Modernize · Architect · Deploy · Migrate · Operate.</summary>
    public string? Stage { get; set; }
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
