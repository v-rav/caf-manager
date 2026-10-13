namespace CafPortal.Domain.Entities.Governance;

/// <summary>Raw per-nomination tool-usage row imported from the FDO "Nomination Wize Tool Usage" export.
/// One nomination (TPID + Task Id) can have many rows (1-to-many). Portal-owned reporting fact —
/// wiped and reloaded on each drop; never edited in the portal.</summary>
public class NominationToolUsageFact
{
    public int Id { get; set; }

    /// <summary>FDO external Account ID from the export (the "Account ID" column).</summary>
    public string? ExternalAccountId { get; set; }
    public string? Tpid { get; set; }
    /// <summary>FDO Task Id — joins to <see cref="Nomination.ExternalTaskId"/>.</summary>
    public string? TaskId { get; set; }
    /// <summary>Resolved internal nomination (by Task Id) when a match exists.</summary>
    public int? NominationId { get; set; }

    /// <summary>Free-text description of what the tool was used for.</summary>
    public string? Details { get; set; }
    /// <summary>Usage category (e.g. "Tool Used").</summary>
    public string? Category { get; set; }
    /// <summary>Tool name as reported (e.g. "GHCP", "GHCP CI", "GHCP-IaC").</summary>
    public string? ToolName { get; set; }
    public System.DateOnly? CreatedDate { get; set; }

    public Nomination? Nomination { get; set; }
}
