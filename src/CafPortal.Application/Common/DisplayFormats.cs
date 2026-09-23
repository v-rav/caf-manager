using CafPortal.Domain.Enums;

namespace CafPortal.Application.Common;

public static class DisplayFormats
{
    public static string ToDisplay(this CapacityStatusType status) => status switch
    {
        CapacityStatusType.Available => "Available",
        CapacityStatusType.PartiallyUtilized => "Partially Utilized",
        CapacityStatusType.FullyUtilized => "Fully Utilized",
        CapacityStatusType.Overloaded => "Overloaded",
        _ => status.ToString()
    };

    public static string ToDisplay(this NominationStatusType status) => status switch
    {
        NominationStatusType.Open => "Open",
        NominationStatusType.InProgress => "In Progress",
        NominationStatusType.Closed => "Closed",
        _ => status.ToString()
    };
}
