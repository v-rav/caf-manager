using System.Security.Claims;
using CafPortal.Application.Abstractions;
using CafPortal.Domain.Entities.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Auth;

/// <summary>SA-role users may only change nominations they own (SA / PM / CFTL / assigned resource, matched on
/// display name). Reads stay open; Admin and Lead are unrestricted.</summary>
public sealed class SaOwnsNominationAttribute() : TypeFilterAttribute(typeof(Filter))
{
    private sealed class Filter(IApplicationDbContext db) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var user = ctx.HttpContext.User;
            if (HttpMethods.IsGet(ctx.HttpContext.Request.Method) || !user.IsInRole(nameof(UserRole.Sa)))
            {
                await next();
                return;
            }

            var routeId = ctx.RouteData.Values.TryGetValue("nominationId", out var v) ? v : ctx.RouteData.Values.GetValueOrDefault("id");
            if (!int.TryParse(routeId?.ToString(), out var nominationId))
            {
                await next();
                return;
            }

            var me = (user.FindFirstValue("displayName") ?? user.Identity?.Name ?? string.Empty).Trim().ToLower();
            var ct = ctx.HttpContext.RequestAborted;
            var owns = me.Length > 0 && (
                await db.Nominations.AnyAsync(n => n.Id == nominationId && (
                    (n.SolutionArchitect ?? "").Trim().ToLower() == me ||
                    (n.ProjectCoordinator ?? "").Trim().ToLower() == me ||
                    (n.CftlPrimary ?? "").Trim().ToLower() == me), ct)
                || await db.NominationResources.AnyAsync(nr => nr.NominationId == nominationId
                    && db.Resources.Any(r => r.ResourceId == nr.ResourceId && r.Name.Trim().ToLower() == me), ct));

            if (!owns)
            {
                ctx.Result = new ObjectResult(new { message = "You can only change nominations you own." }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
            await next();
        }
    }
}
