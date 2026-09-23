using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities;

public class LeaveFact : AuditableEntity
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public DateOnly LeaveDate { get; set; }
    public string LeaveType { get; set; } = string.Empty;

    public Resource? Resource { get; set; }
}
