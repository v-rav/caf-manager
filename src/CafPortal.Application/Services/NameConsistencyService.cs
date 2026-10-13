using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Deterministic fuzzy name-consistency assist. Clusters likely-same names via union-find over
/// <see cref="NameMatching"/> similarity, and applies approved variants as aliases. No LLM / external calls.
/// </summary>
public class NameConsistencyService(IApplicationDbContext db) : INameConsistencyService
{
    private const int MaxGroups = 250;

    public async Task<NameConsistencyReportDto> GetAccountSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.80);
        var accounts = await db.Accounts.AsNoTracking()
            .Select(a => new { a.AccountId, a.AccountName, a.Tpid })
            .ToListAsync(ct);
        var linkCounts = await db.Nominations.AsNoTracking()
            .Where(n => n.AccountId != null)
            .GroupBy(n => n.AccountId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var nodes = accounts.Select(a => new Node(
            a.AccountId, a.AccountName ?? string.Empty, a.Tpid,
            NameMatching.Canonicalize(a.AccountName), NameMatching.Sequence(a.AccountName),
            linkCounts.TryGetValue(a.AccountId, out var c) ? c : 0)).ToList();

        // Canonical preference: has TPID, then most linked, then longest name.
        var groups = Cluster(nodes, threshold,
            canonicalRank: n => (n.Tpid is { Length: > 0 } ? 1 : 0, n.Linked, n.Name.Length));

        // Drop variants already registered as an alias of their canonical (so applied ones don't resurface).
        var aliasPairs = await db.AccountAliases.AsNoTracking()
            .Select(x => new { x.Alias, x.StandardAccountName }).ToListAsync(ct);
        var known = new HashSet<string>(aliasPairs.Select(p => AliasKey(p.Alias, p.StandardAccountName)), StringComparer.OrdinalIgnoreCase);
        groups = RemoveApplied(groups, (g, v) => known.Contains(AliasKey(v.Name, g.CanonicalName)));

        return new NameConsistencyReportDto("accounts", threshold, groups.Count, groups.Sum(g => g.Variants.Count), groups);
    }

    public async Task<NameConsistencyReportDto> GetResourceSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.80);
        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Psid })
            .ToListAsync(ct);
        var linkCounts = await db.ResourceAccounts.AsNoTracking()
            .GroupBy(ra => ra.ResourceId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var nodes = resources.Select(r => new Node(
            r.ResourceId, r.Name ?? string.Empty, r.Psid,
            NameMatching.Canonicalize(r.Name), NameMatching.Sequence(r.Name),
            linkCounts.TryGetValue(r.ResourceId, out var c) ? c : 0)).ToList();

        // Canonical preference: has PSID, then most linked, then longest name.
        var groups = Cluster(nodes, threshold,
            canonicalRank: n => (n.Tpid is { Length: > 0 } ? 1 : 0, n.Linked, n.Name.Length));

        // Drop variants already present on the canonical resource's alias list.
        var aliasLists = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Aliases }).ToListAsync(ct);
        var aliasByRes = aliasLists.ToDictionary(r => r.ResourceId, r => SplitAliases(r.Aliases));
        groups = RemoveApplied(groups, (g, v) =>
            aliasByRes.TryGetValue(g.CanonicalId, out var list) && list.Contains(v.Name, StringComparer.OrdinalIgnoreCase));

        return new NameConsistencyReportDto("resources", threshold, groups.Count, groups.Sum(g => g.Variants.Count), groups);
    }

    public async Task<NameConsistencyApplyResultDto> ApplyAccountAliasesAsync(ApplyAccountAliasRequest request, CancellationToken ct = default)
    {
        var canonical = (request.CanonicalName ?? string.Empty).Trim();
        if (canonical.Length == 0) return new NameConsistencyApplyResultDto(0, "Canonical name is required.");

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.AccountName == canonical, ct);
        var existingAliases = await db.AccountAliases.Select(x => x.Alias).ToListAsync(ct);
        var known = new HashSet<string>(existingAliases, StringComparer.OrdinalIgnoreCase);

        var written = 0;
        var accountAliasList = SplitAliases(account?.Aliases);
        foreach (var raw in request.Aliases ?? Array.Empty<string>())
        {
            var variant = (raw ?? string.Empty).Trim();
            if (variant.Length == 0 || string.Equals(variant, canonical, StringComparison.OrdinalIgnoreCase)) continue;
            if (!known.Contains(variant))
            {
                db.AccountAliases.Add(new AccountAlias { Alias = variant, StandardAccountName = canonical });
                known.Add(variant);
                written++;
            }
            if (!accountAliasList.Contains(variant, StringComparer.OrdinalIgnoreCase))
                accountAliasList.Add(variant);
        }
        if (account is not null)
            account.Aliases = accountAliasList.Count > 0 ? string.Join("; ", accountAliasList) : account.Aliases;

        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(written,
            written > 0 ? $"Registered {written} alias(es) for \"{canonical}\"." : "No new aliases to register.");
    }

    public async Task<NameConsistencyApplyResultDto> ApplyResourceAliasesAsync(ApplyResourceAliasRequest request, CancellationToken ct = default)
    {
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == request.ResourceId, ct);
        if (resource is null) return new NameConsistencyApplyResultDto(0, "Resource not found.");

        var list = SplitAliases(resource.Aliases);
        var written = 0;
        foreach (var raw in request.Aliases ?? Array.Empty<string>())
        {
            var variant = (raw ?? string.Empty).Trim();
            if (variant.Length == 0 || string.Equals(variant, resource.Name, StringComparison.OrdinalIgnoreCase)) continue;
            if (!list.Contains(variant, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(variant);
                written++;
            }
        }
        resource.Aliases = list.Count > 0 ? string.Join("; ", list) : resource.Aliases;
        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(written,
            written > 0 ? $"Added {written} alias(es) to \"{resource.Name}\"." : "No new aliases to add.");
    }

    public async Task<PerformanceNameReportDto> GetPerformanceSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.78);
        var perf = await db.PerformanceReviews.AsNoTracking()
            .GroupBy(p => p.PersonName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Psid, r.Aliases })
            .ToListAsync(ct);

        // Match targets: each roster name + alias, all pointing back at the resource's primary name.
        var targets = new List<(int Id, string Primary, string? Psid, string Canon, string Seq)>();
        foreach (var r in resources)
        {
            var name = r.Name ?? string.Empty;
            targets.Add((r.ResourceId, name, r.Psid, NameMatching.Canonicalize(name), NameMatching.Sequence(name)));
            foreach (var al in SplitAliases(r.Aliases))
                targets.Add((r.ResourceId, name, r.Psid, NameMatching.Canonicalize(al), NameMatching.Sequence(al)));
        }

        var suggestions = new List<PerformanceNameSuggestionDto>();
        foreach (var p in perf)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) continue;
            var pc = NameMatching.Canonicalize(p.Name);
            var psq = NameMatching.Sequence(p.Name);
            if (pc.Length == 0) continue;

            (int Id, string Primary, string? Psid, double Score)? best = null;
            foreach (var t in targets)
            {
                if (t.Canon.Length == 0) continue;
                var s = NameMatching.Score(pc, psq, t.Canon, t.Seq);
                if (best is null || s > best.Value.Score) best = (t.Id, t.Primary, t.Psid, s);
            }
            if (best is null || best.Value.Score < threshold) continue;

            var b = best.Value;
            // Already consistent: exact (case-insensitive) match, or the same normalized form as the roster primary.
            if (string.Equals(p.Name.Trim(), b.Primary.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            if (NameMatching.Canonicalize(b.Primary) == pc) continue;

            suggestions.Add(new PerformanceNameSuggestionDto(
                p.Name, p.Count, b.Id, b.Primary, b.Psid, Math.Round(b.Score, 3)));
        }

        suggestions = suggestions.OrderByDescending(s => s.Score).ThenBy(s => s.CurrentName).ToList();
        return new PerformanceNameReportDto(threshold, suggestions.Count, suggestions);
    }

    public async Task<NameConsistencyApplyResultDto> ApplyPerformanceNameAsync(ApplyPerformanceNameRequest request, CancellationToken ct = default)
    {
        var current = (request.CurrentName ?? string.Empty).Trim();
        var roster = (request.RosterName ?? string.Empty).Trim();
        if (current.Length == 0 || roster.Length == 0) return new NameConsistencyApplyResultDto(0, "Both names are required.");

        var rows = await db.PerformanceReviews.Where(p => p.PersonName == current).ToListAsync(ct);
        foreach (var r in rows)
        {
            r.PersonName = roster;
            r.UpdatedUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(rows.Count,
            rows.Count > 0 ? $"Renamed {rows.Count} review(s) to \"{roster}\"." : "No matching reviews.");
    }

    public async Task<OwnershipReportDto> GetOwnershipSuggestionsAsync(double threshold, CancellationToken ct = default)
    {
        threshold = Clamp(threshold, 0.70);

        // Distinct FDO ownership names (SA/PM/CFTL) with the roles they appear as and nomination counts.
        var noms = await db.Nominations.AsNoTracking()
            .Select(n => new { n.Id, n.SolutionArchitect, n.CftlPrimary, n.ProjectCoordinator })
            .ToListAsync(ct);
        var owners = new Dictionary<string, (HashSet<string> Roles, HashSet<int> Noms)>(StringComparer.OrdinalIgnoreCase);
        void Add(string? name, string role, int nid)
        {
            var n = (name ?? string.Empty).Trim();
            if (n.Length == 0) return;
            if (!owners.TryGetValue(n, out var e)) owners[n] = e = (new(StringComparer.OrdinalIgnoreCase), new());
            e.Roles.Add(role);
            e.Noms.Add(nid);
        }
        foreach (var n in noms)
        {
            Add(n.SolutionArchitect, "SA", n.Id);
            Add(n.CftlPrimary, "CFTL", n.Id);
            Add(n.ProjectCoordinator, "PM", n.Id);
        }

        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Email, r.Psid, r.Aliases })
            .ToListAsync(ct);
        // Match targets: roster name + each alias, pointing back at the resource's primary name/email.
        var targets = new List<(int Id, string Primary, string? Email, string? Psid, string Canon, string Seq)>();
        var exact = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in resources)
        {
            var name = r.Name ?? string.Empty;
            var canon = NameMatching.Canonicalize(name);
            exact.Add(canon);
            targets.Add((r.ResourceId, name, r.Email, r.Psid, canon, NameMatching.Sequence(name)));
            foreach (var al in SplitAliases(r.Aliases))
            {
                var ac = NameMatching.Canonicalize(al);
                exact.Add(ac);
                targets.Add((r.ResourceId, name, r.Email, r.Psid, ac, NameMatching.Sequence(al)));
            }
        }

        var suggestions = new List<OwnershipSuggestionDto>();
        var matched = 0;
        foreach (var kv in owners)
        {
            var canon = NameMatching.Canonicalize(kv.Key);
            if (canon.Length == 0) continue;
            if (exact.Contains(canon)) { matched++; continue; } // already resolves to a roster person

            var seq = NameMatching.Sequence(kv.Key);
            var byRes = new Dictionary<int, (string Name, string? Email, string? Psid, double Score)>();
            foreach (var t in targets)
            {
                if (t.Canon.Length == 0) continue;
                var s = NameMatching.Score(canon, seq, t.Canon, t.Seq);
                if (!byRes.TryGetValue(t.Id, out var cur) || s > cur.Score) byRes[t.Id] = (t.Primary, t.Email, t.Psid, s);
            }
            var cands = byRes
                .Select(b => new OwnershipCandidateDto(b.Key, b.Value.Name, b.Value.Email, b.Value.Psid, Math.Round(b.Value.Score, 3)))
                .OrderByDescending(c => c.Score)
                .Take(3)
                .ToList();
            suggestions.Add(new OwnershipSuggestionDto(
                kv.Key, kv.Value.Roles.OrderBy(x => x).ToList(), kv.Value.Noms.Count,
                cands.Count > 0 ? cands[0].Score : 0, cands));
        }

        suggestions = suggestions
            .OrderByDescending(s => s.BestScore)
            .ThenByDescending(s => s.NominationCount)
            .Take(MaxGroups)
            .ToList();
        return new OwnershipReportDto(threshold, suggestions.Count, matched, suggestions);
    }

    public async Task<NameConsistencyApplyResultDto> RenameResourceAsync(RenameResourceRequest request, CancellationToken ct = default)
    {
        var newName = (request.NewName ?? string.Empty).Trim();
        if (newName.Length == 0) return new NameConsistencyApplyResultDto(0, "New name is required.");
        var r = await db.Resources.FirstOrDefaultAsync(x => x.ResourceId == request.ResourceId, ct);
        if (r is null) return new NameConsistencyApplyResultDto(0, "Resource not found.");
        var old = r.Name ?? string.Empty;
        if (string.Equals(old, newName, StringComparison.OrdinalIgnoreCase)) return new NameConsistencyApplyResultDto(0, "Name unchanged.");

        // Keep the old spelling as an alias so existing references still resolve.
        var aliases = SplitAliases(r.Aliases);
        if (old.Length > 0 && !aliases.Contains(old, StringComparer.OrdinalIgnoreCase)) aliases.Add(old);
        r.Name = newName;
        r.Aliases = aliases.Count > 0 ? string.Join("; ", aliases) : r.Aliases;
        await db.SaveChangesAsync(ct);
        return new NameConsistencyApplyResultDto(1, $"Renamed roster resource to \"{newName}\" (kept \"{old}\" as alias).");
    }

    public async Task<OwnershipLinkResultDto> LinkSolutionArchitectsAsync(bool apply, CancellationToken ct = default)
    {
        const double autoThreshold = 0.95;
        var settled = new[]
        {
            NominationStatusType.Closed, NominationStatusType.Completed,
            NominationStatusType.Withdrawn, NominationStatusType.CustomerDeferred,
        };

        var resources = await db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Aliases })
            .ToListAsync(ct);
        // Exact canon → resource, plus fuzzy match targets (roster name + aliases).
        var exactMap = new Dictionary<string, int>(StringComparer.Ordinal);
        var targets = new List<(int Id, string Canon, string Seq)>();
        foreach (var r in resources)
        {
            var name = r.Name ?? string.Empty;
            var canon = NameMatching.Canonicalize(name);
            if (canon.Length > 0) { exactMap.TryAdd(canon, r.ResourceId); targets.Add((r.ResourceId, canon, NameMatching.Sequence(name))); }
            foreach (var al in SplitAliases(r.Aliases))
            {
                var ac = NameMatching.Canonicalize(al);
                if (ac.Length > 0) { exactMap.TryAdd(ac, r.ResourceId); targets.Add((r.ResourceId, ac, NameMatching.Sequence(al))); }
            }
        }

        var noms = await db.Nominations.AsNoTracking()
            .Where(n => n.AccountId != null && n.ApprovalStatus == "Approved"
                     && !settled.Contains(n.Status)
                     && n.SolutionArchitect != null && n.SolutionArchitect != "")
            .Select(n => new { n.Id, n.SolutionArchitect })
            .ToListAsync(ct);

        var toLink = new List<(int NomId, int ResId)>();
        var aliasLearn = new Dictionary<int, HashSet<string>>();
        var resolved = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        var unresolved = 0;
        foreach (var n in noms)
        {
            var sa = n.SolutionArchitect!.Trim();
            if (!resolved.TryGetValue(sa, out var rid))
            {
                var canon = NameMatching.Canonicalize(sa);
                if (canon.Length == 0) rid = null;
                else if (exactMap.TryGetValue(canon, out var ex)) rid = ex;
                else
                {
                    var seq = NameMatching.Sequence(sa);
                    int? best = null;
                    double bestScore = 0;
                    foreach (var tgt in targets)
                    {
                        var s = NameMatching.Score(canon, seq, tgt.Canon, tgt.Seq);
                        if (s > bestScore) { bestScore = s; best = tgt.Id; }
                    }
                    if (best is int b && bestScore >= autoThreshold)
                    {
                        rid = b;
                        if (!aliasLearn.TryGetValue(b, out var set)) aliasLearn[b] = set = new(StringComparer.OrdinalIgnoreCase);
                        set.Add(sa); // remember so the next drop is an instant exact-alias hit
                    }
                    else rid = null;
                }
                resolved[sa] = rid;
            }
            if (rid is int res) toLink.Add((n.Id, res));
            else unresolved++;
        }

        var distinctLinks = toLink.Distinct().ToList();
        var created = 0;
        if (apply)
        {
            // Rebuild SA-derived links so they always reflect the current FDO SA. Seed/Portal links are preserved.
            await db.NominationResources.Where(nr => nr.Source == "SA").ExecuteDeleteAsync(ct);

            // The (NominationId, ResourceId) pair is unique; don't duplicate a pair already linked (Seed/Portal).
            var existingPairs = await db.NominationResources.AsNoTracking()
                .Select(nr => new { nr.NominationId, nr.ResourceId })
                .ToListAsync(ct);
            var pairSet = existingPairs.Select(p => (p.NominationId, p.ResourceId)).ToHashSet();

            foreach (var (nomId, resId) in distinctLinks)
                if (pairSet.Add((nomId, resId)))
                {
                    db.NominationResources.Add(new NominationResource { NominationId = nomId, ResourceId = resId, Role = "Solution Architect", Source = "SA" });
                    created++;
                }

            var learnIds = aliasLearn.Keys.ToList();
            if (learnIds.Count > 0)
            {
                var learnResources = await db.Resources.Where(r => learnIds.Contains(r.ResourceId)).ToListAsync(ct);
                foreach (var res in learnResources)
                {
                    var list = SplitAliases(res.Aliases);
                    foreach (var nm in aliasLearn[res.ResourceId])
                        if (!list.Contains(nm, StringComparer.OrdinalIgnoreCase) && !string.Equals(nm, res.Name, StringComparison.OrdinalIgnoreCase))
                            list.Add(nm);
                    res.Aliases = list.Count > 0 ? string.Join("; ", list) : res.Aliases;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        return new OwnershipLinkResultDto(apply, noms.Count, apply ? created : distinctLinks.Count, aliasLearn.Values.Sum(s => s.Count), unresolved);
    }

    // ---- clustering ----

    private sealed record Node(int Id, string Name, string? Tpid, string Canon, string Seq, int Linked);

    /// <summary>Two rows with distinct identity keys (TPID / PSID) are confirmed-different entities, never aliases.</summary>
    private static bool DistinctIdentity(Node a, Node b) =>
        a.Tpid is { Length: > 0 } ta && b.Tpid is { Length: > 0 } tb &&
        !string.Equals(ta, tb, StringComparison.OrdinalIgnoreCase);

    private static List<NameSuggestionDto> Cluster(
        List<Node> nodes, double threshold, Func<Node, (int, int, int)> canonicalRank)
    {
        var n = nodes.Count;
        var parent = new int[n];
        for (var i = 0; i < n; i++) parent[i] = i;

        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Union(int a, int b) { var ra = Find(a); var rb = Find(b); if (ra != rb) parent[ra] = rb; }

        for (var i = 0; i < n; i++)
        {
            if (nodes[i].Canon.Length == 0) continue;
            for (var j = i + 1; j < n; j++)
            {
                if (nodes[j].Canon.Length == 0) continue;
                if (DistinctIdentity(nodes[i], nodes[j])) continue; // different TPID/PSID = different entity
                if (NameMatching.Score(nodes[i].Canon, nodes[i].Seq, nodes[j].Canon, nodes[j].Seq) >= threshold)
                    Union(i, j);
            }
        }

        var components = new Dictionary<int, List<int>>();
        for (var i = 0; i < n; i++)
        {
            if (nodes[i].Canon.Length == 0) continue;
            var root = Find(i);
            (components.TryGetValue(root, out var list) ? list : components[root] = new List<int>()).Add(i);
        }

        var result = new List<NameSuggestionDto>();
        foreach (var members in components.Values)
        {
            if (members.Count < 2) continue;
            var canonicalIdx = members.OrderByDescending(i => canonicalRank(nodes[i])).First();
            var canon = nodes[canonicalIdx];
            var variants = members.Where(i => i != canonicalIdx)
                // A member with its own distinct identity key is a real separate record, not an alias.
                .Where(i => !DistinctIdentity(nodes[i], canon))
                .Select(i => new NameVariantDto(
                    nodes[i].Id, nodes[i].Name, nodes[i].Tpid,
                    Math.Round(NameMatching.Score(nodes[i].Canon, nodes[i].Seq, canon.Canon, canon.Seq), 3),
                    nodes[i].Linked))
                .OrderByDescending(v => v.Score)
                .ToList();
            if (variants.Count == 0) continue;
            result.Add(new NameSuggestionDto(
                canon.Id, canon.Name, canon.Tpid, variants.Min(v => v.Score), variants));
        }

        return result.OrderByDescending(g => g.Variants.Count).ThenByDescending(g => g.MinScore).Take(MaxGroups).ToList();
    }

    private static List<string> SplitAliases(string? aliases) =>
        string.IsNullOrWhiteSpace(aliases)
            ? new List<string>()
            : aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string AliasKey(string? alias, string? standard) =>
        $"{(alias ?? string.Empty).Trim()}\u0001{(standard ?? string.Empty).Trim()}";

    private static List<NameSuggestionDto> RemoveApplied(
        List<NameSuggestionDto> groups, Func<NameSuggestionDto, NameVariantDto, bool> applied)
    {
        var result = new List<NameSuggestionDto>();
        foreach (var g in groups)
        {
            var kept = g.Variants.Where(v => !applied(g, v)).ToList();
            if (kept.Count == 0) continue;
            result.Add(g with { Variants = kept, MinScore = kept.Min(v => v.Score) });
        }
        return result;
    }

    private static double Clamp(double v, double min) => v < min ? min : (v > 0.999 ? 0.999 : v);
}
