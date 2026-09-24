namespace CafPortal.Domain.Enums;

/// <summary>Where a resource is in the onboarding/access lifecycle.</summary>
public enum OnboardingStatusType
{
    NotStarted = 0,
    AccessRequested = 1,
    AccessGranted = 2,
    Trained = 3,
    Active = 4
}
