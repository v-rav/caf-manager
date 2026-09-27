using System.Security.Claims;
using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IUserService users) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await users.ValidateCredentialsAsync(req.Username?.Trim() ?? "", req.Password ?? "", ct);
        if (user is null) return Unauthorized(new { message = "Invalid username or password." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("displayName", user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        await users.MarkLoggedInAsync(user.Id, ct);
        return Ok(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var user = await users.GetAsync(id.Value, ct);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var id = CurrentUserId();
        if (id is null) return Unauthorized();
        var ok = await users.ChangePasswordAsync(id.Value, req.CurrentPassword ?? "", req.NewPassword ?? "", ct);
        return ok ? Ok(new { ok = true }) : BadRequest(new { message = "Current password is incorrect." });
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
