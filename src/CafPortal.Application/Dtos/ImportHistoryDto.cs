namespace CafPortal.Application.Dtos;

/// <summary>Summary of one data-import run for the History list.</summary>
public class ImportRunDto
{
    public int Id { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Withdrawn { get; set; }
    public int Unchanged { get; set; }
}

/// <summary>A single field change within an Updated record.</summary>
public class FieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string? From { get; set; }
    public string? To { get; set; }
}

/// <summary>One row-level change within an import run.</summary>
public class ImportChangeDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? ExternalKey { get; set; }
    public string? Label { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public List<FieldChangeDto> Changes { get; set; } = [];
}
