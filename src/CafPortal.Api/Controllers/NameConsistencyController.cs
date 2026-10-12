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
public class NameConsistencyController(INameConsistencyService service) : ControllerBase
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
        => Ok(await service.ApplyResourceAliasesAsync(request, ct));
}
