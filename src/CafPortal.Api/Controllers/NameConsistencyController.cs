using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Deterministic fuzzy name-consistency review queue (accounts + resources). Admin-only; apply is explicit.</summary>
[ApiController]
[Route("api/name-consistency")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class NameConsistencyController(INameConsistencyService service, ICapacityRebuildService capacityRebuild) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery] double threshold, CancellationToken ct)
        => Ok(await service.GetAccountSuggestionsAsync(threshold <= 0 ? 0.88 : threshold, ct));

    [HttpGet("resources")]
    public async Task<IActionResult> Resources([FromQuery] double threshold, CancellationToken ct)
        => Ok(await service.GetResourceSuggestionsAsync(threshold <= 0 ? 0.90 : threshold, ct));

    [HttpPost("accounts/apply")]
    public async Task<IActionResult> ApplyAccounts([FromBody] ApplyAccountAliasRequest request, CancellationToken ct)
        => Ok(await service.ApplyAccountAliasesAsync(request, ct));

    [HttpPost("resources/apply")]
    public async Task<IActionResult> ApplyResources([FromBody] ApplyResourceAliasRequest request, CancellationToken ct)
    {
        var result = await service.ApplyResourceAliasesAsync(request, ct);
        await ResyncCapacityAsync(ct); // a new alias can resolve an SA → keep capacity current
        return Ok(result);
    }

    [HttpGet("performance")]
    public async Task<IActionResult> Performance([FromQuery] double threshold, CancellationToken ct)
        => Ok(await service.GetPerformanceSuggestionsAsync(threshold <= 0 ? 0.84 : threshold, ct));

    [HttpPost("performance/apply")]
    public async Task<IActionResult> ApplyPerformance([FromBody] ApplyPerformanceNameRequest request, CancellationToken ct)
        => Ok(await service.ApplyPerformanceNameAsync(request, ct));

    [HttpGet("ownership")]
    public async Task<IActionResult> Ownership([FromQuery] double threshold, CancellationToken ct)
        => Ok(await service.GetOwnershipSuggestionsAsync(threshold <= 0 ? 0.80 : threshold, ct));

    [HttpPost("resources/rename")]
    public async Task<IActionResult> RenameResource([FromBody] RenameResourceRequest request, CancellationToken ct)
    {
        var result = await service.RenameResourceAsync(request, ct);
        await ResyncCapacityAsync(ct); // a rename can newly match an SA → keep capacity current
        return Ok(result);
    }

    // Re-link SA → resource, then rebuild the capacity snapshot (so alias/rename edits take effect immediately).
    private async Task ResyncCapacityAsync(CancellationToken ct)
    {
        await service.LinkSolutionArchitectsAsync(true, ct);
        await capacityRebuild.RebuildAllAsync(ct);
    }

    [HttpPost("link-sa")]
    public async Task<IActionResult> LinkSa([FromQuery] bool apply, CancellationToken ct)
    {
        var result = await service.LinkSolutionArchitectsAsync(apply, ct);
        if (apply) await capacityRebuild.RebuildAllAsync(ct);
        return Ok(result);
    }
}
