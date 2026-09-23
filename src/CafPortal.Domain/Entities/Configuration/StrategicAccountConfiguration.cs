using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

public class StrategicAccountConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public int? AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public bool StrategicFlag { get; set; } = true;

    /// <summary>1 = Standard, 2 = Strategic, 3 = Executive Critical.</summary>
    public int PriorityWeight { get; set; } = 2;
    public bool RiskFlag { get; set; }
    public bool ExecutiveVisibilityFlag { get; set; }

    public Account? Account { get; set; }
}
