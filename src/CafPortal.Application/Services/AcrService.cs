using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>ACR rates live as ApplicationSettings (Acr* keys); missing keys fall back to the documented App Factory defaults.</summary>
public class AcrService(IApplicationDbContext db) : IAcrService
{
    private readonly IApplicationDbContext _db = db;

    private static readonly (string Key, Func<AcrRatesDto, decimal> Get, Action<AcrRatesDto, decimal> Set)[] Map =
    {
        ("AcrAnnualizationMonths", r => r.AnnualizationMonths, (r, v) => r.AnnualizationMonths = (int)v),
        ("AcrAppServiceArpuPerCoreMonth", r => r.AppServiceArpuPerCoreMonth, (r, v) => r.AppServiceArpuPerCoreMonth = v),
        ("AcrAppServiceCoresPerApp", r => r.AppServiceCoresPerApp, (r, v) => r.AppServiceCoresPerApp = v),
        ("AcrAksLinuxArpuPerCoreMonth", r => r.AksLinuxArpuPerCoreMonth, (r, v) => r.AksLinuxArpuPerCoreMonth = v),
        ("AcrAksWindowsArpuPerCoreMonth", r => r.AksWindowsArpuPerCoreMonth, (r, v) => r.AksWindowsArpuPerCoreMonth = v),
        ("AcrAksCoresPerApp", r => r.AksCoresPerApp, (r, v) => r.AksCoresPerApp = v),
        ("AcrAcaArpuPerCoreHour", r => r.AcaArpuPerCoreHour, (r, v) => r.AcaArpuPerCoreHour = v),
        ("AcrAcaUtilization", r => r.AcaUtilization, (r, v) => r.AcaUtilization = v),
        ("AcrAcaHoursPerMonth", r => r.AcaHoursPerMonth, (r, v) => r.AcaHoursPerMonth = v),
        ("AcrContainerCoreFloor", r => r.ContainerCoreFloor, (r, v) => r.ContainerCoreFloor = v),
    };

    public async Task<AcrRatesDto> GetRatesAsync(CancellationToken ct = default)
    {
        var stored = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key.StartsWith("Acr"))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        var dto = new AcrRatesDto();
        foreach (var (key, _, set) in Map)
            if (stored.TryGetValue(key, out var v) && decimal.TryParse(v, out var d)) set(dto, d);
        return dto;
    }

    public async Task<AcrRatesDto> SaveRatesAsync(AcrRatesDto rates, CancellationToken ct = default)
    {
        var existing = await _db.ApplicationSettings.Where(s => s.Key.StartsWith("Acr")).ToDictionaryAsync(s => s.Key, ct);
        foreach (var (key, get, _) in Map)
        {
            var value = get(rates).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (existing.TryGetValue(key, out var row)) row.Value = value;
            else _db.ApplicationSettings.Add(new ApplicationSetting { Key = key, Value = value, Description = "ACR calculation rate" });
        }
        await _db.SaveChangesAsync(ct);
        return await GetRatesAsync(ct);
    }

    public async Task<AcrEstimateResult> EstimateAsync(AcrEstimateRequest request, CancellationToken ct = default)
    {
        var r = await GetRatesAsync(ct);
        var months = r.AnnualizationMonths <= 0 ? 12 : r.AnnualizationMonths;
        var svc = (request.TargetService ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

        decimal cores, monthly;
        string formula;
        switch (svc)
        {
            case "appservice":
                cores = request.Cores ?? (request.Apps ?? 0) * r.AppServiceCoresPerApp;
                monthly = cores * r.AppServiceArpuPerCoreMonth;
                formula = $"{cores:0.##} cores × ${r.AppServiceArpuPerCoreMonth:0.##}/core/mo × {months}";
                break;
            case "akswindows":
                cores = request.Cores ?? (request.Apps ?? 0) * r.AksCoresPerApp;
                monthly = cores * r.AksWindowsArpuPerCoreMonth;
                formula = $"{cores:0.##} cores × ${r.AksWindowsArpuPerCoreMonth:0.##}/core/mo × {months}";
                break;
            case "aca":
                cores = request.Cores ?? (request.Apps ?? 0) * r.AksCoresPerApp;
                monthly = r.AcaArpuPerCoreHour * cores * r.AcaUtilization * r.AcaHoursPerMonth;
                formula = $"${r.AcaArpuPerCoreHour:0.###}/core/hr × {cores:0.##} cores × {r.AcaUtilization:0.##} × {r.AcaHoursPerMonth:0.##} hr/mo × {months}";
                break;
            default: // akslinux (default AKS)
                cores = request.Cores ?? (request.Apps ?? 0) * r.AksCoresPerApp;
                monthly = cores * r.AksLinuxArpuPerCoreMonth;
                formula = $"{cores:0.##} cores × ${r.AksLinuxArpuPerCoreMonth:0.##}/core/mo × {months}";
                break;
        }
        return new AcrEstimateResult(request.TargetService, cores, Math.Round(monthly, 2), Math.Round(monthly * months, 2), formula);
    }
}
