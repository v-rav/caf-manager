namespace CafPortal.Domain.Enums;

/// <summary>Capacity utilization band for a resource. Thresholds themselves are configuration-driven.</summary>
public enum CapacityStatusType
{
    Available = 0,
    PartiallyUtilized = 1,
    FullyUtilized = 2,
    Overloaded = 3
}
