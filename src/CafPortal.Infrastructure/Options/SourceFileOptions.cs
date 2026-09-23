namespace CafPortal.Infrastructure.Options;

/// <summary>Locations of the three source workbooks. Bound from the "SourceFiles" config section.</summary>
public class SourceFileOptions
{
    public const string SectionName = "SourceFiles";

    /// <summary>Directory that holds the workbooks (relative paths resolve against ContentRoot).</summary>
    public string Directory { get; set; } = "SourceData";
    public string ResourceFile { get; set; } = "App Migration_Region_wise Mapping.xlsx";
    public string LeaveFile { get; set; } = "LeaveCal.xlsx";
    public string EngagementFile { get; set; } = "Time-hunt_Tracking.xlsx";
    public string NominationFile { get; set; } = "Detail View.xlsx";
}
