using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Domain.Enums;
using CafPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Workflow status behaviour (settled / blocked / tone / selectable) + the Nominations default approval filter.
/// Read by any signed-in user (pages render from it); edits are Admin-only and rebuild capacity.</summary>
[ApiController]
[Route("api/status-rules")]
[Produces("application/json")]
[Authorize]
public class StatusRulesController(IApplicationDbContext db, ICapacityRebuildService rebuild, ICurrentUser currentUser) : ControllerBase
{
    public record RuleUpdate(string Status, bool IsSettled, bool IsBlocked, string Tone, bool Selectable, int SortOrder);
    public record SaveRequest(IReadOnlyList<RuleUpdate> Rules, string? DefaultApproval);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var defaultApproval = await db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == StatusRules.DefaultApprovalKey).Select(s => s.Value).FirstOrDefaultAsync(ct);
        var approvals = await db.Nominations.AsNoTracking()
            .Where(n => n.ApprovalStatus != null && n.ApprovalStatus != "")
            .Select(n => n.ApprovalStatus!).Distinct().OrderBy(a => a).ToListAsync(ct);
        return Ok(new
        {
            Rules = StatusRules.All.Select(r => new
            {
                Status = r.Status.ToString(), Label = r.Status.ToDisplay(),
                r.IsSettled, r.IsBlocked, r.Tone, r.Selectable, r.SortOrder,
            }),
            DefaultApproval = defaultApproval ?? StatusRules.DefaultApprovalValue,
            ApprovalValues = approvals,
            StatusRules.Tones,
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] SaveRequest req, CancellationToken ct)
    {
        if (req.Rules is null || req.Rules.Count == 0) return BadRequest("No rules supplied.");
        foreach (var u in req.Rules)
        {
            if (!Enum.TryParse<NominationStatusType>(u.Status, out _)) return BadRequest($"Unknown status '{u.Status}'.");
            if (!StatusRules.Tones.Contains(u.Tone)) return BadRequest($"Tone must be one of: {string.Join(", ", StatusRules.Tones)}.");
            if (u.IsSettled && u.IsBlocked) return BadRequest($"{u.Status}: a status can't be both settled and blocked.");
        }
        var merged = StatusRules.All.ToDictionary(r => r.Status.ToString(), r => r.IsSettled);
        foreach (var u in req.Rules) merged[u.Status] = u.IsSettled;
        if (merged.Values.All(s => s)) return BadRequest("At least one status must remain in-flight (not settled).");

        var rows = await db.StatusRules.ToListAsync(ct);
        var settledChanged = false;
        foreach (var u in req.Rules)
        {
            var row = rows.FirstOrDefault(r => r.Status == u.Status);
            if (row is null) { row = new StatusRule { Status = u.Status }; db.StatusRules.Add(row); }
            settledChanged |= row.IsSettled != u.IsSettled;
            row.IsSettled = u.IsSettled;
            row.IsBlocked = u.IsBlocked;
            row.Tone = u.Tone;
            row.Selectable = u.Selectable;
            row.SortOrder = u.SortOrder;
            row.UpdatedBy = currentUser.Name;
            row.UpdatedUtc = DateTimeOffset.UtcNow;
        }
        if (req.DefaultApproval is not null)
        {
            var setting = await db.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == StatusRules.DefaultApprovalKey, ct);
            if (setting is null)
                db.ApplicationSettings.Add(new ApplicationSetting { Key = StatusRules.DefaultApprovalKey, Value = req.DefaultApproval.Trim() });
            else
                setting.Value = req.DefaultApproval.Trim();
        }
        await db.SaveChangesAsync(ct);
        await StatusRuleStore.LoadAsync(db, ct);
        // Settled statuses drive the nomination-derived capacity snapshot.
        if (settledChanged) await rebuild.RebuildAllAsync(ct);
        return await Get(ct);
    }
}
