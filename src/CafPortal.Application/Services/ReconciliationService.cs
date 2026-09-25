using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// Builds the read-only reconciliation report for the resource→account mappings.
/// In-flight = an Approved nomination whose status is not settled (Closed/Completed/Withdrawn/CustomerDeferred).
/// Master = the account already carries a TPID. SuggestMerge = a TPID-less link whose name maps (via alias
/// or normalized name) to a master account. Orphan = no TPID and no suggestion.
/// </summary>
public class ReconciliationService(IApplicationDbContext db) : IReconciliationService
{
    private readonly IApplicationDbContext _db = db;

    private static readonly NominationStatusType[] Settled =
    {
        NominationStatusType.Closed,
        NominationStatusType.Completed,
        NominationStatusType.Withdrawn,
        NominationStatusType.CustomerDeferred
    };

    public async Task<ReconciliationReportDto> GetAsync(string? region, CancellationToken ct = default)
    {
        var scoped = !string.IsNullOrWhiteSpace(region) && !region.Equals("Global", StringComparison.OrdinalIgnoreCase);

        // In-flight account set (decision: Approved AND not a settled status).
        var inFlightIds = (await _db.Nominations.AsNoTracking()
                .Where(n => n.AccountId != null && n.ApprovalStatus == "Approved" && !Settled.Contains(n.Status))
                .Select(n => n.AccountId!.Value)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        // Master accounts (carry a TPID) → normalized-name lookup for merge suggestions.
        var masters = await _db.Accounts.AsNoTracking()
            .Where(a => a.Tpid != null && a.Tpid != "")
            .Select(a => new { a.AccountId, a.AccountName, a.Tpid })
            .ToListAsync(ct);
        var masterByNorm = new Dictionary<string, (int Id, string Name, string? Tpid)>();
        foreach (var m in masters)
        {
            var key = Norm(m.AccountName);
            if (key.Length > 0 && !masterByNorm.ContainsKey(key))
                masterByNorm[key] = (m.AccountId, m.AccountName, m.Tpid);
        }
        var masterNorms = masterByNorm.Keys.ToList();

        // Alias → standard name, then standard name → master.
        var aliasMap = await _db.AccountAliases.AsNoTracking()
            .ToDictionaryAsync(a => Norm(a.Alias), a => a.StandardAccountName, ct);

        var linksQuery = _db.ResourceAccounts.AsNoTracking()
            .Include(ra => ra.Resource)
            .Include(ra => ra.Account)
            .AsQueryable();
        if (scoped)
            linksQuery = linksQuery.Where(ra => ra.Resource!.Region == region);

        var links = await linksQuery.ToListAsync(ct);

        var rows = new List<ReconciliationRowDto>(links.Count);
        foreach (var ra in links)
        {
            var acct = ra.Account;
            var res = ra.Resource;
            if (acct is null || res is null) continue;

            var inMaster = !string.IsNullOrWhiteSpace(acct.Tpid);
            var inFlight = inFlightIds.Contains(acct.AccountId);

            string matchState;
            int? sugId = null; string? sugName = null; string? sugTpid = null;

            if (inMaster)
            {
                matchState = "Master";
            }
            else
            {
                var (id, name, tpid) = SuggestMaster(acct.AccountName, aliasMap, masterByNorm, masterNorms);
                if (id is not null)
                {
                    matchState = "SuggestMerge";
                    sugId = id; sugName = name; sugTpid = tpid;
                }
                else
                {
                    matchState = "Orphan";
                }
            }

            rows.Add(new ReconciliationRowDto(
                res.ResourceId, res.Name, res.Region,
                acct.AccountId, acct.AccountName, acct.Tpid, acct.Segment,
                ra.RelationshipType,
                inMaster, inFlight, matchState,
                sugId, sugName, sugTpid,
                inFlight ? "Keep" : "Drop"));
        }

        // Order so the actionable rows surface first: Orphan, then SuggestMerge, then Master; Drop before Keep.
        rows = rows
            .OrderBy(r => r.MatchState == "Orphan" ? 0 : r.MatchState == "SuggestMerge" ? 1 : 2)
            .ThenBy(r => r.UtilizationEffect == "Drop" ? 0 : 1)
            .ThenBy(r => r.ResourceName)
            .ThenBy(r => r.AccountName)
            .ToList();

        var summary = new ReconciliationSummaryDto
        {
            TotalLinks = rows.Count,
            Resources = rows.Select(r => r.ResourceId).Distinct().Count(),
            LinkedAccounts = rows.Select(r => r.AccountId).Distinct().Count(),
            Master = rows.Count(r => r.MatchState == "Master"),
            SuggestMerge = rows.Count(r => r.MatchState == "SuggestMerge"),
            Orphan = rows.Count(r => r.MatchState == "Orphan"),
            Keep = rows.Count(r => r.UtilizationEffect == "Keep"),
            Drop = rows.Count(r => r.UtilizationEffect == "Drop"),
            InFlightAccounts = inFlightIds.Count
        };

        return new ReconciliationReportDto { Summary = summary, Rows = rows };
    }

    private static (int? Id, string? Name, string? Tpid) SuggestMaster(
        string rawName,
        Dictionary<string, string> aliasMap,
        Dictionary<string, (int Id, string Name, string? Tpid)> masterByNorm,
        List<string> masterNorms)
    {
        var norm = Norm(rawName);
        if (norm.Length == 0) return (null, null, null);

        // 1) Alias table maps this name to a canonical name.
        if (aliasMap.TryGetValue(norm, out var standard))
        {
            var sNorm = Norm(standard);
            if (masterByNorm.TryGetValue(sNorm, out var viaAlias))
                return (viaAlias.Id, viaAlias.Name, viaAlias.Tpid);
        }

        // 2) Exact normalized-name match to a master.
        if (masterByNorm.TryGetValue(norm, out var exact))
            return (exact.Id, exact.Name, exact.Tpid);

        // 3) Containment either direction (guard against very short tokens matching everything).
        if (norm.Length >= 5)
        {
            var hit = masterNorms
                .Where(m => m.Length >= 5 && (m.Contains(norm) || norm.Contains(m)))
                .OrderBy(m => Math.Abs(m.Length - norm.Length))
                .FirstOrDefault();
            if (hit is not null && masterByNorm.TryGetValue(hit, out var contained))
                return (contained.Id, contained.Name, contained.Tpid);
        }

        return (null, null, null);
    }

    public async Task<SeedAssignmentResultDto> SeedAssignmentsAsync(string? region, bool apply, CancellationToken ct = default)
    {
        var scoped = !string.IsNullOrWhiteSpace(region) && !region.Equals("Global", StringComparison.OrdinalIgnoreCase);

        // In-flight nominations, carrying the FDO Solution Architect (authoritative, per wave).
        var inflightNoms = (await _db.Nominations.AsNoTracking()
                .Where(n => n.AccountId != null && n.ApprovalStatus == "Approved" && !Settled.Contains(n.Status))
                .Select(n => new { n.Id, AccountId = n.AccountId!.Value, n.AccountName, n.Region, n.SolutionArchitect })
                .ToListAsync(ct))
            .Where(n => !scoped || n.Region == region)
            .ToList();
        var nomsByAccount = inflightNoms.GroupBy(n => n.AccountId).ToDictionary(g => g.Key, g => g.ToList());
        var inflightAccountIds = nomsByAccount.Keys.ToHashSet();

        // Resources: id lookup + normalized-name → id, to match the FDO SA name to a resource.
        var resources = await _db.Resources.AsNoTracking()
            .Select(r => new { r.ResourceId, r.Name, r.Role, r.Aliases }).ToListAsync(ct);
        var resourceNames = resources.ToDictionary(r => r.ResourceId, r => r.Name);
        var resByNorm = new Dictionary<string, int>();
        foreach (var r in resources)
        {
            // Match on the canonical name and any alias (FDO name variants).
            var candidates = new List<string> { r.Name };
            if (!string.IsNullOrWhiteSpace(r.Aliases))
                candidates.AddRange(r.Aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            foreach (var nm in candidates)
            {
                var k = Norm(nm);
                if (k.Length > 0 && !resByNorm.ContainsKey(k)) resByNorm[k] = r.ResourceId;
            }
        }

        // Account links (used ONLY for engineering roles — SA comes from FDO, one per wave).
        var linksQuery = _db.ResourceAccounts.AsNoTracking().Include(ra => ra.Resource).AsQueryable();
        if (scoped)
            linksQuery = linksQuery.Where(ra => ra.Resource!.Region == region);
        var links = (await linksQuery.ToListAsync(ct))
            .Where(ra => ra.Resource != null && inflightAccountIds.Contains(ra.AccountId))
            .ToList();

        // Desired seed set: (nomination, resource) → role.
        var desired = new Dictionary<(int Nom, int Res), (string Role, string? Account)>();
        var saUnmatchedWaves = 0;

        // 1) Solution Architect — one per wave, from the FDO field matched to a resource by name.
        foreach (var n in inflightNoms)
        {
            var sa = (n.SolutionArchitect ?? string.Empty).Trim();
            if (sa.Length == 0) continue;
            if (resByNorm.TryGetValue(Norm(sa), out var rid))
                desired[(n.Id, rid)] = ("Solution Architect", n.AccountName);
            else
                saUnmatchedWaves++;
        }

        // 2) Migration Engineer / DevOps — account-level fan-out to every in-flight wave. Architects excluded here.
        foreach (var ra in links)
        {
            var role = MapRole(ra.Resource!.Role);
            if (role == "Solution Architect") continue;
            foreach (var nom in nomsByAccount[ra.AccountId])
                desired.TryAdd((nom.Id, ra.ResourceId), (role, nom.AccountName));
        }

        // Existing rows: manual ("Portal") are preserved; seed-managed (null or "Seed") are replaced on apply.
        var existing = await _db.NominationResources
            .Select(x => new { x.Id, x.NominationId, x.ResourceId, x.Source })
            .ToListAsync(ct);
        var portalKeys = existing.Where(x => x.Source == "Portal")
            .Select(x => (x.NominationId, x.ResourceId)).ToHashSet();
        var seedRowIds = existing.Where(x => x.Source == null || x.Source == "Seed").Select(x => x.Id).ToList();

        // Never override a manual (Portal) assignment for the same (nomination, resource).
        var toCreate = desired.Where(d => !portalKeys.Contains((d.Key.Nom, d.Key.Res))).ToList();

        if (apply)
        {
            if (seedRowIds.Count > 0)
            {
                var idSet = seedRowIds.ToHashSet();
                var remove = await _db.NominationResources.Where(x => idSet.Contains(x.Id)).ToListAsync(ct);
                _db.NominationResources.RemoveRange(remove); // removes only seed-origin assignment rows; nominations untouched
            }
            foreach (var d in toCreate)
                _db.NominationResources.Add(new NominationResource
                {
                    NominationId = d.Key.Nom,
                    ResourceId = d.Key.Res,
                    Role = d.Value.Role,
                    Source = "Seed"
                });
            await _db.SaveChangesAsync(ct);
        }

        var sample = toCreate
            .OrderBy(d => d.Value.Role == "Solution Architect" ? 0 : 1)
            .Take(25)
            .Select(d => new SeedAssignmentRowDto(
                d.Key.Nom, d.Value.Account, d.Key.Res,
                resourceNames.TryGetValue(d.Key.Res, out var nm) ? nm : string.Empty,
                d.Value.Role, false))
            .ToList();

        return new SeedAssignmentResultDto
        {
            Applied = apply,
            InFlightLinks = links.Count,
            Candidates = desired.Count,
            WouldCreate = toCreate.Count,
            Created = apply ? toCreate.Count : 0,
            RemovedSeed = seedRowIds.Count,
            SaCreated = toCreate.Count(d => d.Value.Role == "Solution Architect"),
            EngineerCreated = toCreate.Count(d => d.Value.Role != "Solution Architect"),
            SaUnmatchedWaves = saUnmatchedWaves,
            SkippedExisting = desired.Count - toCreate.Count,
            ResourcesAffected = toCreate.Select(d => d.Key.Res).Distinct().Count(),
            NominationsAffected = toCreate.Select(d => d.Key.Nom).Distinct().Count(),
            Sample = sample
        };
    }

    public async Task<UnmatchedPeopleResultDto> GetUnmatchedPeopleAsync(string? region, CancellationToken ct = default)
    {
        var scoped = !string.IsNullOrWhiteSpace(region) && !region.Equals("Global", StringComparison.OrdinalIgnoreCase);

        // Alias-aware set of portal resource names.
        var resources = await _db.Resources.AsNoTracking()
            .Select(r => new { r.Name, r.Aliases }).ToListAsync(ct);
        var known = new HashSet<string>();
        foreach (var r in resources)
        {
            var names = new List<string> { r.Name };
            if (!string.IsNullOrWhiteSpace(r.Aliases))
                names.AddRange(r.Aliases.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            foreach (var nm in names)
            {
                var k = Norm(nm);
                if (k.Length > 0) known.Add(k);
            }
        }

        // In-flight nominations carrying the three FDO ownership fields.
        var noms = (await _db.Nominations.AsNoTracking()
                .Where(n => n.ApprovalStatus == "Approved" && !Settled.Contains(n.Status))
                .Select(n => new { n.Region, n.AccountName, n.SolutionArchitect, n.ProjectCoordinator, n.CftlPrimary })
                .ToListAsync(ct))
            .Where(n => !scoped || n.Region == region)
            .ToList();

        // (normalized name, role) -> aggregate of wave count, regions, sample accounts.
        var agg = new Dictionary<(string Norm, string Role), (string Display, int Count, HashSet<string> Regions, HashSet<string> Accounts)>();
        void Add(string? name, string role, string? regionVal, string? account)
        {
            var nm = (name ?? string.Empty).Trim();
            if (nm.Length == 0) return;
            var k = Norm(nm);
            if (k.Length == 0 || known.Contains(k)) return; // already a portal resource
            var key = (k, role);
            if (!agg.TryGetValue(key, out var cur))
                cur = (nm, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            cur.Count++;
            if (!string.IsNullOrWhiteSpace(regionVal)) cur.Regions.Add(regionVal!);
            if (!string.IsNullOrWhiteSpace(account)) cur.Accounts.Add(account!);
            agg[key] = cur;
        }
        foreach (var n in noms)
        {
            Add(n.SolutionArchitect, "Solution Architect", n.Region, n.AccountName);
            Add(n.ProjectCoordinator, "Project Coordinator (PM)", n.Region, n.AccountName);
            Add(n.CftlPrimary, "CFTL", n.Region, n.AccountName);
        }

        var people = agg
            .Select(kv => new UnmatchedPersonDto(
                kv.Value.Display,
                kv.Key.Role,
                kv.Value.Count,
                string.Join(", ", kv.Value.Regions.OrderBy(x => x)),
                string.Join("; ", kv.Value.Accounts.Take(3))))
            .OrderByDescending(p => p.WaveCount)
            .ThenBy(p => p.Name)
            .ToList();

        return new UnmatchedPeopleResultDto
        {
            TotalPeople = people.Select(p => Norm(p.Name)).Distinct().Count(),
            TotalReferences = people.Sum(p => p.WaveCount),
            People = people
        };
    }

    public async Task<LinkCleanupResultDto> CleanupLinksAsync(string? region, bool apply, CancellationToken ct = default)
    {
        var report = await GetAsync(region, ct);
        var merges = report.Rows
            .Where(r => r.MatchState == "SuggestMerge" && r.SuggestedAccountId is > 0)
            .ToList();

        var repointed = 0;
        var deduped = 0;
        if (apply)
        {
            foreach (var m in merges)
            {
                var link = await _db.ResourceAccounts
                    .FirstOrDefaultAsync(x => x.ResourceId == m.ResourceId && x.AccountId == m.AccountId, ct);
                if (link is null)
                    continue;
                var target = m.SuggestedAccountId!.Value;
                var targetLinked = await _db.ResourceAccounts
                    .AnyAsync(x => x.ResourceId == m.ResourceId && x.AccountId == target, ct);
                if (targetLinked)
                {
                    _db.ResourceAccounts.Remove(link); // master already linked — drop the duplicate
                    deduped++;
                }
                else
                {
                    link.AccountId = target;
                    repointed++;
                }
            }
            await _db.SaveChangesAsync(ct);
        }

        return new LinkCleanupResultDto
        {
            Applied = apply,
            Mergeable = merges.Count,
            Repointed = apply ? repointed : 0,
            Deduped = apply ? deduped : 0,
            Orphans = report.Summary.Orphan
        };
    }

    // Resource role → delivery role for the seeded assignment (mirror of the picker's eligibility map).
    private static string MapRole(string? resourceRole)
    {
        var r = (resourceRole ?? string.Empty).ToLowerInvariant();
        if (r.Contains("architect")) return "Solution Architect";
        if (r.Contains("devops")) return "DevOps Engineer";
        return "Migration Engineer";
    }

    private static string Norm(string? s) =>
        new string((s ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
