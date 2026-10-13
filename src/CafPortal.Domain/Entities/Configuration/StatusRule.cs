using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Behaviour of one nomination workflow status (the status set itself is fixed by the enum):
/// whether it is settled (stops counting as in-flight/capacity/SLA), blocked, its badge tone, and whether it can be
/// picked in the Manage dialog. Edited in Configuration → Workflow status rules.</summary>
public class StatusRule : AuditableEntity
{
    public int Id { get; set; }
    /// <summary>NominationStatusType name (e.g. CustomerDeferred).</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsSettled { get; set; }
    public bool IsBlocked { get; set; }
    /// <summary>Badge tone: success · warning · danger · informative · brand · subtle.</summary>
    public string Tone { get; set; } = "informative";
    public bool Selectable { get; set; } = true;
    public int SortOrder { get; set; }
    public string? UpdatedBy { get; set; }
}
