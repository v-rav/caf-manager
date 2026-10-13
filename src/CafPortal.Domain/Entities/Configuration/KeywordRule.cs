using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>An admin-editable classification rule: "if text matches Pattern, the Domain classifies as Target".
/// Replaces hardcoded keyword maps (stage, health, wave type, tool aliases, activities, container paths).</summary>
public class KeywordRule : AuditableEntity
{
    public int Id { get; set; }
    /// <summary>Stage · Health · WaveType · ToolAlias · Activity · ContainerPath.</summary>
    public string Domain { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    /// <summary>Contains (case-insensitive substring) or Exact (case/space-insensitive equality).</summary>
    public string MatchType { get; set; } = "Contains";
    public string Target { get; set; } = string.Empty;
    /// <summary>Lower runs first; first-match domains stop at the first hit.</summary>
    public int Priority { get; set; }
    public bool ActiveFlag { get; set; } = true;
    public string? UpdatedBy { get; set; }
}
