namespace CafPortal.Application.Dtos;

/// <summary>Migration analytics aggregated over Approved nominations (region-scoped). Amount lists carry sums.</summary>
public class AnalyticsDto
{
    public int TotalApproved { get; set; }
    public int InFlight { get; set; }
    public int Completed { get; set; }

    public double TotalAcr { get; set; }
    public double NnrAcr { get; set; }
    public double TotalCores { get; set; }
    /// <summary>How many Approved nominations carry ACR data (coverage from the Offerings enrichment).</summary>
    public int WithAcr { get; set; }

    public int ToolAttached { get; set; }
    public int AutomationUsed { get; set; }
    /// <summary>Nominations that carry a Yes/No tool-or-automation flag (the % denominator).</summary>
    public int ToolFlagDenom { get; set; }

    /// <summary>Average kick-off -> actual-migration-start lag (days) over nominations that recorded both milestones.</summary>
    public double AvgKickoffToStartLagDays { get; set; }
    /// <summary>How many nominations have both milestone dates (the lag denominator).</summary>
    public int WithKickoffLag { get; set; }

    // Count distributions.
    public IReadOnlyList<NameValueDto> ByStage { get; set; } = [];
    public IReadOnlyList<NameValueDto> ByHealth { get; set; } = [];
    public IReadOnlyList<NameValueDto> BySla { get; set; } = [];
    public IReadOnlyList<NameValueDto> ByRegion { get; set; } = [];
    public IReadOnlyList<NameValueDto> BySegment { get; set; } = [];
    public IReadOnlyList<NameValueDto> ByMigrationPath { get; set; } = [];
    public IReadOnlyList<NameValueDto> ByModeOfAccess { get; set; } = [];
    public IReadOnlyList<NameValueDto> WaveLinkage { get; set; } = [];

    // Value (ACR / cores) distributions.
    public IReadOnlyList<NameValueDto> AcrByRegion { get; set; } = [];
    public IReadOnlyList<NameValueDto> AcrBySegment { get; set; } = [];
    public IReadOnlyList<NameValueDto> AcrByMigrationPath { get; set; } = [];
    public IReadOnlyList<NameValueDto> CoresByStage { get; set; } = [];
    public IReadOnlyList<NameValueDto> TopPartnersByAcr { get; set; } = [];
    /// <summary>ACR summed by approval step across ALL statuses (Concierge / Provisional / Approved / Declined …).</summary>
    public IReadOnlyList<NameValueDto> AcrByApproval { get; set; } = [];
}
