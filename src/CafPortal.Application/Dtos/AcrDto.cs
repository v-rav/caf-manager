namespace CafPortal.Application.Dtos;

/// <summary>Editable FDO ACR calculation rates (App Factory thumb-rules). All annualized via AnnualizationMonths.</summary>
public class AcrRatesDto
{
    public int AnnualizationMonths { get; set; } = 12;
    public decimal AppServiceArpuPerCoreMonth { get; set; } = 98m;
    public decimal AppServiceCoresPerApp { get; set; } = 2m;
    public decimal AksLinuxArpuPerCoreMonth { get; set; } = 30m;
    public decimal AksWindowsArpuPerCoreMonth { get; set; } = 56m;
    public decimal AksCoresPerApp { get; set; } = 4m;
    public decimal AcaArpuPerCoreHour { get; set; } = 0m;
    public decimal AcaUtilization { get; set; } = 0.8m;
    public decimal AcaHoursPerMonth { get; set; } = 730m;
}

/// <summary>targetService: AppService · AksLinux · AksWindows · Aca. Supply apps and/or cores.</summary>
public record AcrEstimateRequest(string TargetService, decimal? Apps, decimal? Cores);

public record AcrEstimateResult(string TargetService, decimal Cores, decimal MonthlyAcr, decimal AnnualAcr, string Formula);
