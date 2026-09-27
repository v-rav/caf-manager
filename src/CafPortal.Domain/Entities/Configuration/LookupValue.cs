using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Generic editable vocabulary value (blocker categories, classifications, velocity impacts, blocker owners).
/// One table, many categories — admins add/edit/delete without a code change.</summary>
public class LookupValue : AuditableEntity
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool ActiveFlag { get; set; } = true;
}
