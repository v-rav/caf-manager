namespace CafPortal.Application.Dtos;

public class CapacityRowDto
{
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int AccountCount { get; set; }
    public int CapacityLimit { get; set; }
    public double UtilizationPercent { get; set; }
    public string CapacityStatus { get; set; } = string.Empty;
    /// <summary>Heatmap color hint: Green / Amber / Red.</summary>
    public string HeatColor { get; set; } = "Green";
}
