using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Configured account segment (e.g. Strategic, Upper Majors). Data-driven so new ones need no code change.</summary>
public class SegmentConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
