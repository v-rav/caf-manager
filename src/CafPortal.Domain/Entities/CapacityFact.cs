using CafPortal.Domain.Common;
using CafPortal.Domain.Enums;

namespace CafPortal.Domain.Entities;

/// <summary>Materialized capacity snapshot rebuilt by the capacity engine.</summary>
public class CapacityFact : AuditableEntity
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public int AccountCount { get; set; }
    public int CapacityLimit { get; set; }
    public double UtilizationPercent { get; set; }
    public CapacityStatusType CapacityStatus { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    public Resource? Resource { get; set; }
}
