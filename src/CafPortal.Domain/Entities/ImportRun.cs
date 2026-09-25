namespace CafPortal.Domain.Entities;

/// <summary>One data-import run (FDO upload or scheduled refresh). Records what changed for the History page.</summary>
public class ImportRun
{
    public int Id { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }

    /// <summary>Which dataset was imported, e.g. "nominations".</summary>
    public string Source { get; set; } = "nominations";

    /// <summary>Original uploaded file name, when triggered by an in-app upload.</summary>
    public string? FileName { get; set; }

    public int Added { get; set; }
    public int Updated { get; set; }
    public int Withdrawn { get; set; }
    public int Unchanged { get; set; }

    public ICollection<ImportChange> Changes { get; set; } = new List<ImportChange>();
}

/// <summary>A single row-level change recorded during an <see cref="ImportRun"/>.</summary>
public class ImportChange
{
    public int Id { get; set; }
    public int ImportRunId { get; set; }
    public ImportRun? Run { get; set; }

    public string EntityType { get; set; } = "Nomination";

    /// <summary>Stable external key (FDO Task Id) of the affected record.</summary>
    public string? ExternalKey { get; set; }

    /// <summary>Human-readable label (account · offering) for display.</summary>
    public string? Label { get; set; }

    /// <summary>Added | Updated | Withdrawn.</summary>
    public string ChangeType { get; set; } = "Updated";

    /// <summary>JSON array of {field, from, to} for Updated changes.</summary>
    public string? ChangedFields { get; set; }
}
