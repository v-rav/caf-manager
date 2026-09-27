using System.Security.Claims;
using CafPortal.Application.Abstractions;

namespace CafPortal.Api.Auth;

/// <summary>Reads the authenticated user from the request cookie for updated-by stamping.</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int? Id => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public string? Name => User?.Identity?.Name;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
