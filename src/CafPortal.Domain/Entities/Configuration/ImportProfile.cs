using CafPortal.Domain.Common;

namespace CafPortal.Domain.Entities.Configuration;

/// <summary>An FDO workbook type and its header signature (how an uploaded file is recognised).</summary>
public class ImportProfile : AuditableEntity
{
    public int Id { get; set; }
    /// <summary>nominations · offerings · accounts · toolusage (routes to the matching importer).</summary>
    public string Kind { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>Lower is tried first (Offerings must precede Account master — they share TPID/Customer).</summary>
    public int DetectOrder { get; set; }
    /// <summary>Clauses separated by ';' that must all match; alternatives within a clause separated by '|'.</summary>
    public string Signature { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}

/// <summary>Header candidates for one field of an import ('|'-separated, first header containing any candidate wins).</summary>
public class ImportColumnMap : AuditableEntity
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Candidates { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? UpdatedBy { get; set; }
}
