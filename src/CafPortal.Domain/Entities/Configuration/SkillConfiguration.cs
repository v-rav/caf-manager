using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Controlled vocabulary for skills (data-driven; new skills need no code change).</summary>
public class SkillConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
