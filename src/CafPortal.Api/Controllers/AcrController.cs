using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

/// <summary>Editable FDO ACR rate master + estimator (App Factory thumb-rules).</summary>
[ApiController]
[Route("api/acr")]
[Produces("application/json")]
[Authorize]
public class AcrController(IAcrService acr) : ControllerBase
{
    [HttpGet("rates")]
    public async Task<IActionResult> GetRates(CancellationToken ct) => Ok(await acr.GetRatesAsync(ct));

    [HttpPut("rates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveRates([FromBody] AcrRatesDto rates, CancellationToken ct)
        => Ok(await acr.SaveRatesAsync(rates, ct));

    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate([FromBody] AcrEstimateRequest req, CancellationToken ct)
        => Ok(await acr.EstimateAsync(req, ct));
}
