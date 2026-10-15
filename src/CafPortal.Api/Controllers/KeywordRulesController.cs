using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Domain.Entities.Configuration;
using CafPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Api.Controllers;

/// <summary>Admin CRUD + impact preview for the data-driven classification rules (KeywordRules).</summary>
[ApiController]
[Route("api/keyword-rules")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class KeywordRulesController(IApplicationDbContext db, ICurrentUser currentUser) : ControllerBase
{
    public record RuleInput(string Domain, string Pattern, string MatchType, string Target, int Priority, bool? Active);
    public record PreviewRequest(string Op, int? Id, RuleInput? Rule);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var domains = new List<object>();
        foreach (var d in KeywordRules.Domains)
            domains.Add(new
            {
                d.Key, d.Label, d.Source, d.AllMatches, d.Fallback, d.Description,
                Targets = await TargetsForAsync(d, ct),
            });
        var rules = await db.KeywordRules.AsNoTracking()
            .OrderBy(r => r.Domain).ThenBy(r => r.Priority).ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.Domain, r.Pattern, r.MatchType, r.Target, r.Priority, Active = r.ActiveFlag, r.UpdatedBy, r.UpdatedUtc })
            .ToListAsync(ct);
        return Ok(new { Domains = domains, Rules = rules });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RuleInput input, CancellationToken ct)
    {
        if (await ValidateAsync(input, ct) is { } error) return BadRequest(error);
        var rule = new KeywordRule();
        Apply(rule, input);
        db.KeywordRules.Add(rule);
        await db.SaveChangesAsync(ct);
        await KeywordRuleStore.LoadAsync(db, ct);
        return Ok(new { rule.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RuleInput input, CancellationToken ct)
    {
        var rule = await db.KeywordRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        if (await ValidateAsync(input, ct) is { } error) return BadRequest(error);
        Apply(rule, input);
        rule.UpdatedUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await KeywordRuleStore.LoadAsync(db, ct);
        return Ok(new { rule.Id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var rule = await db.KeywordRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rule is null) return NotFound();
        db.KeywordRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        await KeywordRuleStore.LoadAsync(db, ct);
        return NoContent();
    }

    /// <summary>Shows how a proposed create/update/delete would reclassify the real data, before saving.</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest req, CancellationToken ct)
    {
        var existing = await db.KeywordRules.AsNoTracking().ToListAsync(ct);
        var target = req.Id is int id ? existing.FirstOrDefault(r => r.Id == id) : null;
        var domainKey = req.Rule?.Domain ?? target?.Domain;
        var domain = domainKey is null ? null : KeywordRules.FindDomain(domainKey);
        if (domain is null) return BadRequest("Unknown rule domain.");
        if (req.Op != "delete" && req.Rule is not null && await ValidateAsync(req.Rule, ct) is { } error) return BadRequest(error);

        static KeywordRuleItem Item(KeywordRule r) => new(r.Domain, r.Pattern, r.MatchType, r.Target, r.Priority);
        var current = existing.Where(r => r.ActiveFlag).Select(Item).ToList();
        var candidate = existing
            .Where(r => !(req.Op is "update" or "delete" && r.Id == req.Id))
            .Where(r => r.ActiveFlag)
            .Select(Item).ToList();
        if (req.Op is "create" or "update" && req.Rule is { } ri && (ri.Active ?? true))
            candidate.Add(new KeywordRuleItem(domain.Key, ri.Pattern.Trim(), ri.MatchType, ri.Target.Trim(), ri.Priority));

        var corpus = await CorpusAsync(domain.Key, ct);
        string Describe(IReadOnlyList<string> hits) => hits.Count > 0 ? string.Join(", ", hits) : domain.Fallback ?? "(no match)";
        var diffs = corpus
            .Select(c => new
            {
                Text = c.Key,
                Count = c.Value,
                From = Describe(KeywordRules.Evaluate(current, domain.Key, c.Key)),
                To = Describe(KeywordRules.Evaluate(candidate, domain.Key, c.Key)),
            })
            .Where(x => x.From != x.To)
            .OrderByDescending(x => x.Count)
            .ToList();
        return Ok(new
        {
            Domain = domain.Key,
            DistinctValues = corpus.Count,
            TotalRecords = corpus.Values.Sum(),
            ChangedValues = diffs.Count,
            ChangedRecords = diffs.Sum(d => d.Count),
            Samples = diffs.Take(25),
        });
    }

    // Distinct source text → record count for the domain's input field.
    private async Task<Dictionary<string, int>> CorpusAsync(string domain, CancellationToken ct)
    {
        IQueryable<string?> source = domain switch
        {
            KeywordRules.Stage => db.Nominations.Select(n => n.MigrationStatus),
            KeywordRules.Health => db.Nominations.Select(n => n.CurrentState),
            KeywordRules.ContainerPath => db.Nominations.Select(n => n.PrimaryMigrationPath),
            KeywordRules.ToolAlias => db.NominationToolUsageFacts.Select(u => u.ToolName),
            KeywordRules.Activity => db.NominationToolUsageFacts.Select(u => (u.ToolName ?? "") + " " + (u.Details ?? "")),
            KeywordRules.WaveType => db.WaveLinks.Where(w => w.Source == "FDO").Select(w => w.Reference),
            KeywordRules.BlockerReason => db.Nominations.Select(n => n.Remarks ?? n.CurrentState),
            KeywordRules.SegmentClass => db.Nominations.Where(n => n.Classification == null || n.Classification == "")
                .Select(n => n.Account != null ? n.Account.Segment : null),
            KeywordRules.ActivityPhase => db.MigrationActivities.Where(a => a.ActiveFlag).Select(a => (string?)a.Name),
            _ => Enumerable.Empty<string?>().AsQueryable(),
        };
        var values = await source.Where(v => v != null && v != "").ToListAsync(ct);
        return values.GroupBy(v => v!.Trim()).ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<string[]?> TargetsForAsync(RuleDomain d, CancellationToken ct) => d.Key switch
    {
        KeywordRules.ToolAlias => await db.MigrationTools.AsNoTracking().Where(t => t.ActiveFlag)
            .OrderBy(t => t.Name).Select(t => t.Name).ToArrayAsync(ct),
        KeywordRules.Activity => await db.MigrationActivities.AsNoTracking().Where(a => a.ActiveFlag)
            .OrderBy(a => a.SortOrder).Select(a => a.Name).ToArrayAsync(ct),
        KeywordRules.SegmentClass => await db.LookupValues.AsNoTracking().Where(l => l.Category == "Classification")
            .OrderBy(l => l.SortOrder).Select(l => l.Value).ToArrayAsync(ct),
        _ => d.FixedTargets,
    };

    private async Task<string?> ValidateAsync(RuleInput input, CancellationToken ct)
    {
        var domain = KeywordRules.FindDomain(input.Domain ?? "");
        if (domain is null) return "Unknown rule domain.";
        if (string.IsNullOrWhiteSpace(input.Pattern)) return "Pattern is required.";
        if (input.MatchType is not ("Contains" or "Exact")) return "Match type must be Contains or Exact.";
        var targets = await TargetsForAsync(domain, ct);
        if (string.IsNullOrWhiteSpace(input.Target) || (targets is not null && !targets.Contains(input.Target.Trim())))
            return $"Target must be one of: {string.Join(", ", targets ?? Array.Empty<string>())}.";
        if (input.Priority is < 0 or > 100000) return "Priority must be between 0 and 100000.";
        return null;
    }

    private void Apply(KeywordRule rule, RuleInput input)
    {
        rule.Domain = KeywordRules.FindDomain(input.Domain)!.Key;
        rule.Pattern = input.Pattern.Trim();
        rule.MatchType = input.MatchType;
        rule.Target = input.Target.Trim();
        rule.Priority = input.Priority;
        if (input.Active is bool active) rule.ActiveFlag = active;
        rule.UpdatedBy = currentUser.Name;
    }
}
