using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Excel (.xlsx) downloads for the main grids and an executive summary.</summary>
[ApiController]
[Route("api/[controller]")]
public class ExportController(IExportService export) : ControllerBase
{
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static string Stamp => DateTime.UtcNow.ToString("yyyyMMdd");

    [HttpGet("resources")]
    public async Task<IActionResult> Resources([FromQuery] string? region, CancellationToken ct)
        => File(await export.ResourcesAsync(region, ct), Xlsx, $"resources_{Stamp}.xlsx");

    [HttpGet("capacity")]
    public async Task<IActionResult> Capacity([FromQuery] string? region, CancellationToken ct)
        => File(await export.CapacityAsync(region, ct), Xlsx, $"capacity_{Stamp}.xlsx");

    [HttpGet("nominations")]
    public async Task<IActionResult> Nominations([FromQuery] string? region, [FromQuery] string? approval,
        [FromQuery] string? migrationStatus, [FromQuery] string? currentState, [FromQuery] string? sla,
        [FromQuery] string? links, [FromQuery] string? search, CancellationToken ct)
        => File(await export.NominationsAsync(region, approval, migrationStatus, currentState, sla, links, search, ct), Xlsx, $"nominations_{Stamp}.xlsx");

    [HttpGet("performance")]
    public async Task<IActionResult> Performance([FromQuery] string? region, CancellationToken ct)
        => File(await export.PerformanceAsync(region, ct), Xlsx, $"performance_{Stamp}.xlsx");

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? region, CancellationToken ct)
        => File(await export.ExecutiveSummaryAsync(region, ct), Xlsx, $"executive-summary_{Stamp}.xlsx");
}
