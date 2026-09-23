using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>Per-role capacity limit and threshold overrides used by the capacity engine.</summary>
public class CapacityConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int CapacityLimit { get; set; } = 5;

    /// <summary>Utilization percent at/above which a resource is "Fully Utilized" warning.</summary>
    public double WarningThreshold { get; set; } = 80;

    /// <summary>Utilization percent above which a resource is "Overloaded".</summary>
    public double OverloadedThreshold { get; set; } = 100;
}
