namespace CafPortal.Domain.Enums;

/// <summary>Why a nomination is blocked. Values reflect CAF/FDO governance taxonomy.</summary>
public enum BlockerReasonType
{
    None = 0,
    WaitingForCustomerAction = 1,
    ApprovalPending = 2,
    AccessPending = 3,
    LandingZonePending = 4,
    TestingValidationPending = 5,
    DependencyPending = 6,
    BudgetPriorityHold = 7,
    InternalAlignment = 8
}
