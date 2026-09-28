using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/access")]
[Produces("application/json")]
[Authorize]
public class AccessController(IAccessService access) : ControllerBase
{
    // Any signed-in user can read the matrix (the SPA uses it to build the nav/guards).
    [HttpGet("pages")]
    public async Task<IActionResult> GetPages(CancellationToken ct) => Ok(await access.GetPagesAsync(ct));

    [HttpPut("pages")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SavePages([FromBody] IReadOnlyList<PageAccessUpdate> updates, CancellationToken ct)
        => Ok(await access.SaveAsync(updates ?? [], ct));
}
