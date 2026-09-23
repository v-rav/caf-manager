namespace CafPortal.Application.Dtos;

/// <summary>Filters accepted by the resource hub query.</summary>
public class ResourceQuery
{
    public string? Search { get; set; }
    public string? Region { get; set; }
    public string? Role { get; set; }
    public string? Skill { get; set; }
    public string? Status { get; set; }
}
