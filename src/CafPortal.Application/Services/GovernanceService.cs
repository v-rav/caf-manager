using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class GovernanceService(IApplicationDbContext db, ICurrentUser currentUser) : IGovernanceService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    public async Task<NominationGovernanceDto?> GetForNominationAsync(int nominationId, CancellationToken ct = default)
    {
        var nom = await _db.Nominations.AsNoTracking()
            .Where(n => n.Id == nominationId)
            .Select(n => new
            {
                n.Id, n.AccountName, Tpid = n.Account != null ? n.Account.Tpid : null, n.GhcpAdoptionLevel,
                n.Classification, n.MigrationStatus, n.StageAgeDays, n.ProjectCoordinator, n.CftlPrimary, n.SolutionArchitect, n.ShortName,
            })
            .FirstOrDefaultAsync(ct);
        if (nom is null) return null;

        var gates = await _db.GateDefinitions.AsNoTracking()
            .Where(g => g.Active)
            .Include(g => g.Items.Where(i => i.Active))
            .OrderBy(g => g.Order)
            .ToListAsync(ct);

        var state = await _db.NominationGateItems.AsNoTracking()
            .Where(x => x.NominationId == nominationId)
            .ToDictionaryAsync(x => x.GateItemDefinitionId, ct);

        var openBlockers = await _db.NominationBlockers.AsNoTracking()
            .Where(b => b.NominationId == nominationId && b.ResolvedUtc == null)
            .OrderByDescending(b => b.BlockedSinceUtc)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var clockStoppedDays = openBlockers.Where(b => b.ClockStopped)
            .Sum(b => Math.Max(0, (int)(now - b.BlockedSinceUtc).TotalDays));

        var milestones = await _db.NominationMilestones.AsNoTracking()
            .Where(x => x.NominationId == nominationId)
            .OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.Id)
            .Select(x => new MilestoneDto(x.Id, x.MilestoneKey, x.OccurredOn, x.ToolUsed, x.Notes, x.RecordedBy))
            .ToListAsync(ct);

        var toolUsages = await (
            from u in _db.NominationToolUsages.AsNoTracking().Where(u => u.NominationId == nominationId)
            join t in _db.MigrationTools.AsNoTracking() on u.ToolId equals t.Id
            join a in _db.MigrationActivities.AsNoTracking() on u.ActivityId equals a.Id into aj
            from a in aj.DefaultIfEmpty()
            orderby t.Category, t.Name
            select new ToolUsageDto(
                u.Id, u.NominationId, u.ToolId, t.Name, t.Category, t.Vendor,
                u.ActivityId, a != null ? a.Name : null, a != null ? a.Stage : null,
                u.Stage, u.UsedOn, u.UsedBy, u.Notes))
            .ToListAsync(ct);

        return Build(nom.Id, nom.AccountName, nom.Tpid, gates, state, openBlockers, nom.GhcpAdoptionLevel) with
        {
            Classification = nom.Classification,
            Stage = StageNum(nom.MigrationStatus),
            Pm = nom.ProjectCoordinator,
            Cftl = nom.CftlPrimary,
            Sa = nom.SolutionArchitect,
            AgeDays = nom.StageAgeDays,
            ClockStoppedDays = clockStoppedDays,
            ShortName = nom.ShortName,
            Milestones = milestones,
            ToolUsages = toolUsages,
        };
    }

    private static int? StageNum(string? migration)
    {
        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("executing migration")) return 4;
        if (m.Contains("finalize")) return 3;
        if (m.Contains("pre-requisite") || m.Contains("prerequisite") || m.Contains("pre requisite")) return 2;
        if (m.Contains("validating")) return 1;
        return null;
    }

    public static IReadOnlyList<string> BlockerCategories { get; } = new[]
    {
        "Awaiting GHCP License", "Awaiting Customer Approval", "Awaiting Repository Access",
        "Awaiting Environment Access", "Awaiting Landing Zone", "Awaiting Security Review",
        "Awaiting Customer Testing", "Awaiting PM", "Awaiting Partner", "Internal Factory Dependency",
    };

    public IReadOnlyList<string> GetBlockerCategories() => BlockerCategories;

    public async Task<NominationGovernanceDto?> RaiseBlockerAsync(int nominationId, RaiseBlockerRequest req, CancellationToken ct = default)
    {
        if (!await _db.Nominations.AnyAsync(n => n.Id == nominationId, ct)) return null;
        var category = string.IsNullOrWhiteSpace(req.Category) ? "Internal Factory Dependency" : req.Category;
        _db.NominationBlockers.Add(new NominationBlocker
        {
            NominationId = nominationId,
            GateItemDefinitionId = req.GateItemDefId,
            Category = category,
            ClockStopped = req.ClockStopped,
            Owner = req.Owner,
            ExpectedResolutionUtc = req.ExpectedResolutionUtc,
            Notes = req.Notes,
            BlockedSinceUtc = DateTime.UtcNow,
            RaisedBy = _currentUser.Name,
        });
        Log(nominationId, "BlockerRaised", category, null, req.ClockStopped ? "clock-stopped" : "clock-running", req.GateItemDefId);
        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<NominationGovernanceDto?> ResolveBlockerAsync(int nominationId, int blockerId, CancellationToken ct = default)
    {
        var blocker = await _db.NominationBlockers.FirstOrDefaultAsync(b => b.Id == blockerId && b.NominationId == nominationId, ct);
        if (blocker is null) return null;
        if (blocker.ResolvedUtc is null)
        {
            blocker.ResolvedUtc = DateTime.UtcNow;
            blocker.ResolvedBy = _currentUser.Name;
            Log(nominationId, "BlockerResolved", blocker.Category, null, null, blocker.GateItemDefinitionId);
            await _db.SaveChangesAsync(ct);
        }
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<IReadOnlyList<NominationEventDto>> GetEventsAsync(int nominationId, CancellationToken ct = default)
        => await _db.NominationEvents.AsNoTracking()
            .Where(e => e.NominationId == nominationId)
            .OrderByDescending(e => e.AtUtc).ThenByDescending(e => e.Id)
            .Select(e => new NominationEventDto(e.Id, e.NominationId, e.GateItemDefinitionId, e.Type, e.Field, e.OldValue, e.NewValue, e.ByUser, e.AtUtc))
            .ToListAsync(ct);

    public async Task<NominationGovernanceDto?> AddMilestoneAsync(int nominationId, MilestoneUpsert req, CancellationToken ct = default)
    {
        if (!await _db.Nominations.AnyAsync(n => n.Id == nominationId, ct)) return null;
        if (string.IsNullOrWhiteSpace(req.MilestoneKey)) return await GetForNominationAsync(nominationId, ct);
        _db.NominationMilestones.Add(new NominationMilestone
        {
            NominationId = nominationId,
            MilestoneKey = req.MilestoneKey.Trim(),
            OccurredOn = req.OccurredOn,
            ToolUsed = string.IsNullOrWhiteSpace(req.ToolUsed) ? null : req.ToolUsed.Trim(),
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            RecordedBy = _currentUser.Name,
        });
        Log(nominationId, "Milestone", req.MilestoneKey, null, $"{req.OccurredOn:yyyy-MM-dd}{(string.IsNullOrWhiteSpace(req.ToolUsed) ? "" : $" · {req.ToolUsed}")}", null);
        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<NominationGovernanceDto?> DeleteMilestoneAsync(int nominationId, int milestoneId, CancellationToken ct = default)
    {
        var ms = await _db.NominationMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId && m.NominationId == nominationId, ct);
        if (ms is null) return null;
        _db.NominationMilestones.Remove(ms);
        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<IReadOnlyList<MigrationToolDto>> GetMigrationToolsAsync(CancellationToken ct = default)
    {
        var tools = await _db.MigrationTools.AsNoTracking().Where(t => t.ActiveFlag)
            .OrderBy(t => t.Category).ThenBy(t => t.SortOrder).ThenBy(t => t.Name)
            .ToListAsync(ct);
        var map = await _db.MigrationToolActivities.AsNoTracking()
            .Select(m => new { m.ToolId, m.ActivityId }).ToListAsync(ct);
        var byTool = map.GroupBy(m => m.ToolId).ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.ActivityId).ToList());
        // Empty list = supports any activity (no capability rows defined for the tool).
        return tools.Select(t => new MigrationToolDto(
            t.Id, t.Name, t.Category, t.Vendor, t.SortOrder, t.ActiveFlag,
            byTool.TryGetValue(t.Id, out var ids) ? ids : Array.Empty<int>())).ToList();
    }

    public async Task<IReadOnlyList<MigrationActivityDto>> GetMigrationActivitiesAsync(CancellationToken ct = default)
        => await _db.MigrationActivities.AsNoTracking().Where(a => a.ActiveFlag)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .Select(a => new MigrationActivityDto(a.Id, a.Name, a.Stage, a.SortOrder, a.ActiveFlag))
            .ToListAsync(ct);

    public async Task<CapabilityUtilizationDto> GetCapabilityAsync(string? region, CancellationToken ct = default)
    {
        var scoped = !string.IsNullOrWhiteSpace(region) && !region.Equals("Global View", StringComparison.OrdinalIgnoreCase);
        var rows = await (
            from u in _db.NominationToolUsages.AsNoTracking()
            join n in _db.Nominations.AsNoTracking() on u.NominationId equals n.Id
            join t in _db.MigrationTools.AsNoTracking() on u.ToolId equals t.Id
            join a in _db.MigrationActivities.AsNoTracking() on u.ActivityId equals a.Id into aj
            from a in aj.DefaultIfEmpty()
            where !scoped || n.Region == region
            select new { u.NominationId, ToolName = t.Name, t.Category, ActivityName = a != null ? a.Name : null })
            .ToListAsync(ct);

        var byTool = rows.GroupBy(r => r.ToolName)
            .Select(g => new NameValueDto(g.Key, g.Select(x => x.NominationId).Distinct().Count()))
            .OrderByDescending(x => x.Value).ToArray();
        var byCategory = rows.GroupBy(r => r.Category)
            .Select(g => new NameValueDto(g.Key, g.Select(x => x.NominationId).Distinct().Count()))
            .OrderByDescending(x => x.Value).ToArray();
        var byActivity = rows.Where(r => r.ActivityName != null).GroupBy(r => r.ActivityName!)
            .Select(g => new NameValueDto(g.Key, g.Select(x => x.NominationId).Distinct().Count()))
            .OrderByDescending(x => x.Value).ToArray();

        // Most-used tool per activity: for each activity, the tool with the most distinct nominations.
        var mostUsed = rows.Where(r => r.ActivityName != null)
            .GroupBy(r => r.ActivityName!)
            .Select(g =>
            {
                var top = g.GroupBy(x => x.ToolName)
                    .Select(tg => new { Tool = tg.Key, Count = tg.Select(x => x.NominationId).Distinct().Count() })
                    .OrderByDescending(x => x.Count).First();
                return new CapabilityOutcomeDto(g.Key, top.Tool, top.Count);
            })
            .OrderByDescending(x => x.Nominations).ToList();

        return new CapabilityUtilizationDto(
            rows.Count,
            rows.Select(r => r.NominationId).Distinct().Count(),
            byTool, byCategory, byActivity, mostUsed);
    }

    public async Task<NominationGovernanceDto?> AddToolUsageAsync(int nominationId, ToolUsageUpsert req, CancellationToken ct = default)
    {
        if (!await _db.Nominations.AnyAsync(n => n.Id == nominationId, ct)) return null;
        var tool = await _db.MigrationTools.AsNoTracking().FirstOrDefaultAsync(t => t.Id == req.ToolId, ct);
        if (tool is null) return await GetForNominationAsync(nominationId, ct);
        var activityName = req.ActivityId is int aid
            ? await _db.MigrationActivities.AsNoTracking().Where(a => a.Id == aid).Select(a => a.Name).FirstOrDefaultAsync(ct)
            : null;
        _db.NominationToolUsages.Add(new NominationToolUsage
        {
            NominationId = nominationId,
            ToolId = req.ToolId,
            ActivityId = req.ActivityId,
            Stage = req.Stage,
            UsedOn = req.UsedOn,
            UsedBy = _currentUser.Name,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
        });
        Log(nominationId, "ToolUsage", tool.Name, null, activityName, null);
        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<NominationGovernanceDto?> DeleteToolUsageAsync(int nominationId, int usageId, CancellationToken ct = default)
    {
        var u = await _db.NominationToolUsages.FirstOrDefaultAsync(x => x.Id == usageId && x.NominationId == nominationId, ct);
        if (u is null) return null;
        _db.NominationToolUsages.Remove(u);
        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    public async Task<bool> ApplyAcrCaptureAsync(int nominationId, AcrApplyRequest req, CancellationToken ct = default)
    {
        var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == nominationId, ct);
        if (n is null) return false;
        var oldCores = n.TotalCores;
        var oldAcr = n.TotalAcr;
        if (req.Cores > 0) n.TotalCores = req.Cores;
        if (req.Acr is not null) n.TotalAcr = req.Acr;
        Log(nominationId, "AcrOverride", "TotalCores", oldCores?.ToString(), n.TotalCores?.ToString(), null);
        Log(nominationId, "AcrOverride", "TotalAcr", oldAcr?.ToString(), n.TotalAcr?.ToString(), null);
        if (!string.IsNullOrWhiteSpace(req.Reason))
            Log(nominationId, "AcrOverride", "Reason", null, req.Reason, null);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private void Log(int nominationId, string type, string? field, string? oldValue, string? newValue, int? gateItemDefId)
        => _db.NominationEvents.Add(new NominationEvent
        {
            NominationId = nominationId, Type = type, Field = field, OldValue = oldValue, NewValue = newValue,
            GateItemDefinitionId = gateItemDefId, ByUser = _currentUser.Name, AtUtc = DateTime.UtcNow,
        });

    public async Task<IReadOnlyList<BlockerDto>> GetOpenBlockersAsync(string? region, CancellationToken ct = default)
    {
        var rows = await (from b in _db.NominationBlockers.AsNoTracking()
                          where b.ResolvedUtc == null
                          join n in _db.Nominations.AsNoTracking() on b.NominationId equals n.Id into nj
                          from n in nj.DefaultIfEmpty()
                          where region == null || region == "" || (n != null && n.Region == region)
                          select new { b, Account = n != null ? n.AccountName : null }).ToListAsync(ct);
        var now = DateTime.UtcNow;
        return rows
            .Select(x => new BlockerDto(x.b.Id, x.b.NominationId, x.Account, x.b.GateItemDefinitionId, x.b.Category,
                x.b.ClockStopped, x.b.Owner, x.b.BlockedSinceUtc, x.b.ExpectedResolutionUtc, x.b.ResolvedUtc, x.b.Notes,
                (int)(now - x.b.BlockedSinceUtc).TotalDays, x.b.RaisedBy))
            .OrderByDescending(x => x.DaysBlocked)
            .ToList();
    }

    public async Task<NominationGovernanceDto?> UpdateItemAsync(int nominationId, int itemDefId, UpdateGateItemRequest req, CancellationToken ct = default)
    {
        var nomExists = await _db.Nominations.AnyAsync(n => n.Id == nominationId, ct);
        var itemDef = await _db.GateItemDefinitions.FirstOrDefaultAsync(i => i.Id == itemDefId, ct);
        if (!nomExists || itemDef is null) return null;

        if (!Enum.TryParse<GateItemStatus>(req.Status, ignoreCase: true, out var status))
            status = GateItemStatus.Pending;

        var row = await _db.NominationGateItems
            .FirstOrDefaultAsync(x => x.NominationId == nominationId && x.GateItemDefinitionId == itemDefId, ct);
        var oldStatus = row is null ? "Pending" : row.Status.ToString();
        if (row is null)
        {
            row = new NominationGateItem { NominationId = nominationId, GateItemDefinitionId = itemDefId };
            _db.NominationGateItems.Add(row);
        }

        row.Status = status;
        row.Owner = req.Owner;
        row.Ref = req.Ref;
        row.Notes = req.Notes;
        row.CompletedUtc = status == GateItemStatus.Done ? (row.CompletedUtc ?? DateTime.UtcNow) : null;
        row.UpdatedBy = _currentUser.Name;
        row.UpdatedUtc = DateTime.UtcNow;

        if (oldStatus != status.ToString())
            Log(nominationId, "ItemStatus", itemDef.Label, oldStatus, status.ToString(), itemDefId);

        await _db.SaveChangesAsync(ct);
        return await GetForNominationAsync(nominationId, ct);
    }

    private static NominationGovernanceDto Build(int nominationId, string? account, string? tpid,
        List<GateDefinition> gates, IReadOnlyDictionary<int, NominationGateItem> state, List<NominationBlocker> openBlockers,
        int? adoptionLevel)
    {
        static string StatusName(GateItemStatus s) => s switch
        {
            GateItemStatus.Done => "Done",
            GateItemStatus.NotApplicable => "NA",
            _ => "Pending",
        };

        var blockedItemDefs = openBlockers.Where(b => b.GateItemDefinitionId != null)
            .Select(b => b.GateItemDefinitionId!.Value).ToHashSet();

        var gateDtos = new List<GateDto>();
        double weightedSum = 0, totalWeight = 0;
        string? currentGateKey = null;
        var grp = new Dictionary<string, (int counted, int done)>
        {
            ["Readiness"] = (0, 0), ["Scope"] = (0, 0), ["Delivery"] = (0, 0), ["Signoff"] = (0, 0),
        };

        foreach (var g in gates.OrderBy(g => g.Order))
        {
            var items = g.Items.OrderBy(i => i.Order).Select(i =>
            {
                state.TryGetValue(i.Id, out var s);
                return new GateItemDto(
                    i.Id, i.Key, i.Label, i.Kind.ToString(), i.SubStage, i.ResponsibleRole, i.Mandatory, i.Order,
                    StatusName(s?.Status ?? GateItemStatus.Pending), s?.Owner, s?.CompletedUtc, s?.Ref, s?.Notes,
                    s?.UpdatedBy, s?.UpdatedUtc, blockedItemDefs.Contains(i.Id));
            }).ToList();

            var counted = items.Where(x => x.Status != "NA").ToList();
            var done = counted.Count(x => x.Status == "Done");
            var pct = counted.Count == 0 ? 0 : (int)Math.Round(100.0 * done / counted.Count);
            var status = counted.Count > 0 && done == counted.Count ? "Green" : done > 0 ? "InProgress" : "NotStarted";

            var gk = MsiCalculator.GroupFor(g.Key);
            var acc = grp[gk];
            grp[gk] = (acc.counted + counted.Count, acc.done + done);

            if (currentGateKey is null && status != "Green") currentGateKey = g.Key;
            weightedSum += g.Weight * pct;
            totalWeight += g.Weight;

            gateDtos.Add(new GateDto(g.Key, g.Name, g.ExitCriteria, g.Order, g.Weight, status, pct, items));
        }

        var now = DateTime.UtcNow;
        var blockerDtos = openBlockers
            .Select(b => new BlockerDto(b.Id, b.NominationId, account, b.GateItemDefinitionId, b.Category, b.ClockStopped,
                b.Owner, b.BlockedSinceUtc, b.ExpectedResolutionUtc, b.ResolvedUtc, b.Notes,
                (int)(now - b.BlockedSinceUtc).TotalDays, b.RaisedBy))
            .ToList();

        var compliance = totalWeight > 0 ? (int)Math.Round(weightedSum / totalWeight) : 0;
        currentGateKey ??= gateDtos.LastOrDefault()?.Key;
        int Pct((int counted, int done) x) => x.counted == 0 ? 0 : (int)Math.Round(100.0 * x.done / x.counted);
        var msi = MsiCalculator.Compute(
            Pct(grp["Readiness"]), Pct(grp["Scope"]), Pct(grp["Delivery"]),
            MsiCalculator.RiskHealth(openBlockers.Count, openBlockers.Any(b => b.ClockStopped)),
            MsiCalculator.GhcpScore(adoptionLevel), Pct(grp["Signoff"]));
        return new NominationGovernanceDto(nominationId, account, tpid, null, currentGateKey, compliance, gateDtos, blockerDtos, msi.Score, msi.Band);
    }
}
