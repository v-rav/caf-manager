using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Controlled vocabulary for tool names (replaces free-text entry — the worst offender for data drift).</summary>
public class ToolConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
