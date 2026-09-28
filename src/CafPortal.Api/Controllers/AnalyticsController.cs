using CafPortal.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController(IAnalyticsService analytics) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await analytics.GetAsync(region, ct));

    [HttpGet("timeseries")]
    public async Task<IActionResult> TimeSeries([FromQuery] string? region, [FromQuery] string basis = "completed",
        [FromQuery] string granularity = "month", [FromQuery] string measure = "count", [FromQuery] string? splitBy = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, [FromQuery] int? fy = null, CancellationToken ct = default)
        => Ok(await analytics.GetTimeSeriesAsync(region, basis, granularity, measure, splitBy, from, to, fy, ct));

    [HttpGet("timeseries/detail")]
    public async Task<IActionResult> TimeSeriesDetail([FromQuery] string? region, [FromQuery] string basis,
        [FromQuery] string granularity, [FromQuery] string bucket, [FromQuery] string? splitBy = null,
        [FromQuery] string? series = null, CancellationToken ct = default)
        => Ok(await analytics.GetTimeSeriesDetailAsync(region, basis, granularity, bucket, splitBy, series, ct));

    [HttpGet("attainment")]
    public async Task<IActionResult> Attainment([FromQuery] string? region, [FromQuery] int? fy = null, CancellationToken ct = default)
        => Ok(await analytics.GetAttainmentAsync(region, fy, ct));

    [HttpGet("acr-capture")]
    public async Task<IActionResult> AcrCapture([FromQuery] string? region, CancellationToken ct = default)
        => Ok(await analytics.GetAcrCaptureAsync(region, ct));
}
