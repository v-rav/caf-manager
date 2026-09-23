namespace CafPortal.Application.Dtos;

public class ExecutiveDashboardDto
{
    public int TotalResources { get; set; }
    public int ActiveAccounts { get; set; }
    public int AvailableResources { get; set; }
    public int PartiallyUtilizedResources { get; set; }
    public int FullyUtilizedResources { get; set; }
    public int OverloadedResources { get; set; }
    public int ResourcesOnLeave { get; set; }
    public int StrategicAccounts { get; set; }
    public int OpenNominations { get; set; }

    public IReadOnlyList<NameValueDto> RegionDistribution { get; set; } = Array.Empty<NameValueDto>();
    public IReadOnlyList<NameValueDto> CapacityDistribution { get; set; } = Array.Empty<NameValueDto>();
    public IReadOnlyList<NameValueDto> StrategicAccountCoverage { get; set; } = Array.Empty<NameValueDto>();
}
