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
        NominationStatusType.Blocked => "Blocked",
        NominationStatusType.WaitingForCustomerAction => "Waiting for Customer Action",
        NominationStatusType.CustomerDeferred => "Customer Deferred",
        NominationStatusType.WaitingOnFollowUp => "Waiting on Follow-up",
        NominationStatusType.Completed => "Completed",
        NominationStatusType.Withdrawn => "Withdrawn",
        _ => status.ToString()
    };

    public static string ToDisplay(this BlockerReasonType reason) => reason switch
    {
        BlockerReasonType.None => "None",
        BlockerReasonType.WaitingForCustomerAction => "Waiting for Customer Action",
        BlockerReasonType.ApprovalPending => "Approval Pending",
        BlockerReasonType.AccessPending => "Access Pending",
        BlockerReasonType.LandingZonePending => "Landing Zone Pending",
        BlockerReasonType.TestingValidationPending => "Testing/Validation Pending",
        BlockerReasonType.DependencyPending => "Dependency Pending",
        BlockerReasonType.BudgetPriorityHold => "Budget/Priority Hold",
        BlockerReasonType.InternalAlignment => "Internal Alignment",
        _ => reason.ToString()
    };

    public static string ToDisplay(this WaveType wave) => wave switch
    {
        WaveType.App => "App",
        WaveType.Db => "DB",
        WaveType.Security => "Security/Defender",
        WaveType.LandingZone => "Landing Zone",
        WaveType.Dispatch => "Dispatch",
        WaveType.Related => "Related",
        _ => wave.ToString()
    };

    public static string ToDisplay(this OnboardingStatusType status) => status switch
    {
        OnboardingStatusType.NotStarted => "Not Started",
        OnboardingStatusType.AccessRequested => "Access Requested",
        OnboardingStatusType.AccessGranted => "Access Granted",
        OnboardingStatusType.Trained => "Trained",
        OnboardingStatusType.Active => "Active",
        _ => status.ToString()
    };
}
