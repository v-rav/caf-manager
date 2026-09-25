using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PerformanceController(IPerformanceReviewService performance) : ControllerBase
{
    /// <summary>Latest review per person (region-filtered) with trend + training flags.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? region, CancellationToken ct)
        => Ok(await performance.GetLatestAsync(region, ct));

    /// <summary>Full dated review history for one person.</summary>
    [HttpGet("{personName}/history")]
    public async Task<IActionResult> History(string personName, CancellationToken ct)
        => Ok(await performance.GetHistoryAsync(personName, ct));

    /// <summary>Add a new dated review snapshot (re-score at intervals).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PerformanceReviewUpsertDto input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.PersonName))
            return BadRequest("Person name is required.");
        return Ok(await performance.CreateAsync(input, ct));
    }
}
