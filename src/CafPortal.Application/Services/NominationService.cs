using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class NominationService(IApplicationDbContext db) : INominationService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<IReadOnlyList<NominationDto>> GetAsync(string? region, string? status, CancellationToken ct = default)
    {
        var (warn, escalate, defer) = await GetStaleTiersAsync(ct);
        var (green, amber, red) = await GetStrategicThresholdsAsync(ct);

        var query = _db.Nominations.AsNoTracking()
            .Include(n => n.WaveLinks)
            .Include(n => n.Account)
            .Include(n => n.ResourceAssignments).ThenInclude(ra => ra.Resource)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(n => n.Region == region);
        if (!string.IsNullOrWhiteSpace(status) && TryParseStatus(status, out var parsed))
            query = query.Where(n => n.Status == parsed);

        var items = await query.OrderByDescending(n => n.OpenedDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Clock-stopped blockers pause the SLA clock: subtract their elapsed window from the stage age.
        var nomIds = items.Select(n => n.Id).ToList();
        var nowUtc = DateTime.UtcNow;
        var stops = await _db.NominationBlockers.AsNoTracking()
            .Where(b => b.ClockStopped && nomIds.Contains(b.NominationId))
            .Select(b => new { b.NominationId, b.BlockedSinceUtc, b.ResolvedUtc })
            .ToListAsync(ct);
        var openCounts = await _db.NominationBlockers.AsNoTracking()
            .Where(b => b.ResolvedUtc == null && nomIds.Contains(b.NominationId))
            .GroupBy(b => b.NominationId)
            .Select(g => new { NominationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.NominationId, x => x.Count, ct);
        var stopsByNom = stops.GroupBy(b => b.NominationId).ToDictionary(g => g.Key, g => g.ToList());

        // Gate state for MSI: which item-defs belong to each MSI group, and each nomination's item statuses.
        var gateDefs = await _db.GateDefinitions.AsNoTracking()
            .Where(g => g.Active)
            .Include(g => g.Items.Where(i => i.Active))
            .ToListAsync(ct);
        var groupDefs = new Dictionary<string, HashSet<int>>
        {
            ["Readiness"] = new(), ["Scope"] = new(), ["Delivery"] = new(), ["Signoff"] = new(),
        };
        foreach (var g in gateDefs)
            foreach (var i in g.Items)
                groupDefs[MsiCalculator.GroupFor(g.Key)].Add(i.Id);
        var gateState = await _db.NominationGateItems.AsNoTracking()
            .Where(x => nomIds.Contains(x.NominationId))
            .Select(x => new { x.NominationId, x.GateItemDefinitionId, x.Status })
            .ToListAsync(ct);
        var stateByNom = gateState.GroupBy(x => x.NominationId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.GateItemDefinitionId, x => x.Status));

        int GroupPct(IReadOnlyDictionary<int, Domain.Entities.Governance.GateItemStatus>? st, HashSet<int> defs)
        {
            int counted = 0, done = 0;
            foreach (var id in defs)
            {
                var s = st != null && st.TryGetValue(id, out var v) ? v : Domain.Entities.Governance.GateItemStatus.Pending;
                if (s == Domain.Entities.Governance.GateItemStatus.NotApplicable) continue;
                counted++;
                if (s == Domain.Entities.Governance.GateItemStatus.Done) done++;
            }
            return counted == 0 ? 0 : (int)Math.Round(100.0 * done / counted);
        }

        return items.Select(n =>
        {
            var lastTouch = n.UpdatedUtc ?? n.CreatedUtc;
            var updateDays = Math.Max(0, today.DayNumber - DateOnly.FromDateTime(lastTouch.UtcDateTime).DayNumber);
            // Age = days in the current migration stage when available; else fall back to update recency.
            var days = n.StageAgeDays ?? updateDays;
            // Sum clock-stopped windows (open → now, resolved → resolved), capped so effective age stays >= 0.
            var clockStoppedDays = 0;
            var clockStopped = false;
            if (stopsByNom.TryGetValue(n.Id, out var wins))
                foreach (var w in wins)
                {
                    var end = w.ResolvedUtc ?? nowUtc;
                    clockStoppedDays += Math.Max(0, (int)(end - w.BlockedSinceUtc).TotalDays);
                    if (w.ResolvedUtc is null) clockStopped = true;
                }
            var effectiveDays = Math.Max(0, days - Math.Min(days, clockStoppedDays));
            var classification = string.IsNullOrWhiteSpace(n.Classification) ? "Standard Factory" : n.Classification!;
            var isStrategic = !classification.Equals("Standard Factory", StringComparison.OrdinalIgnoreCase);
            var inFlightDays = Math.Max(0, today.DayNumber - (n.NominatedDate ?? n.OpenedDate).DayNumber);
            var strategicTier = StrategicTier(isStrategic, n.Status, inFlightDays, green, amber, red);

            var openBlk = openCounts.TryGetValue(n.Id, out var oc) ? oc : 0;
            var st = stateByNom.TryGetValue(n.Id, out var s2) ? s2 : null;
            var msi = MsiCalculator.Compute(
                GroupPct(st, groupDefs["Readiness"]), GroupPct(st, groupDefs["Scope"]), GroupPct(st, groupDefs["Delivery"]),
                MsiCalculator.RiskHealth(openBlk, clockStopped), MsiCalculator.GhcpScore(n.GhcpAdoptionLevel),
                GroupPct(st, groupDefs["Signoff"]));
            // Wave links are informational (no required type); surface presence so empty records can be found.
            var waveTypes = n.WaveLinks.Select(w => w.WaveType).ToHashSet();
            var dbLinked = waveTypes.Contains(WaveType.Db);
            var alzLinked = waveTypes.Contains(WaveType.LandingZone) || waveTypes.Contains(WaveType.Dispatch);
            var securityLinked = waveTypes.Contains(WaveType.Security);
            return new NominationDto
            {
                Id = n.Id,
                AccountId = n.AccountId,
                AccountName = n.AccountName ?? n.Account?.AccountName,
                ShortName = n.ShortName,
                Tpid = n.Account?.Tpid,
                Technology = n.Technology,
                Region = n.Region,
                Status = n.Status.ToDisplay(),
                OpenedDate = n.OpenedDate,
                Remarks = n.Remarks,
                MigrationStatus = n.MigrationStatus,
                ApprovalStatus = n.ApprovalStatus,
                StageAgeDays = n.StageAgeDays,
                CurrentState = n.CurrentState,
                SolutionArchitect = n.SolutionArchitect,
                CftlPrimary = n.CftlPrimary,
                ProjectCoordinator = n.ProjectCoordinator,
                Classification = classification,
                VelocityImpact = n.VelocityImpact,
                GhcpAdoptionLevel = n.GhcpAdoptionLevel,
                IsStrategic = isStrategic,
                DaysInFlight = inFlightDays,
                StrategicTier = strategicTier,
                BlockedReason = n.BlockedReason?.ToDisplay(),
                BlockedSince = n.BlockedSince,
                FollowUpDate = n.FollowUpDate,
                PrimaryMigrationPath = n.PrimaryMigrationPath,
                PartnerName = n.PartnerName,
                TotalCores = n.TotalCores,
                IsToolAttached = n.IsToolAttached,
                IsAutomationUsed = n.IsAutomationUsed,
                ModeOfAccess = n.ModeOfAccess,
                TotalAcr = n.TotalAcr,
                NnrAcr = n.NnrAcr,
                NominatedDate = n.NominatedDate,
                ApprovalDate = n.ApprovalDate,
                ActualStartDate = n.ActualStartDate,
                ActualEndDate = n.ActualEndDate,
                PlannedStartDate = n.PlannedStartDate,
                PlannedEndDate = n.PlannedEndDate,
                TotalDays = n.TotalDays,
                DaysSinceUpdate = days,
                EffectiveAgeDays = effectiveDays,
                ClockStopped = clockStopped,
                OpenBlockerCount = openBlk,
                MsiScore = msi.Score,
                MsiBand = msi.Band,
                MsiReadiness = msi.Readiness,
                MsiScope = msi.Scope,
                MsiDelivery = msi.Delivery,
                MsiRisk = msi.Risk,
                MsiGhcp = msi.Ghcp,
                MsiSignoff = msi.Signoff,
                StaleTier = StaleTier(n.Status, StageIndex(n.MigrationStatus), effectiveDays, warn, escalate, defer),
                DbLinked = dbLinked,
                AlzLinked = alzLinked,
                SecurityLinked = securityLinked,
                WaveCount = n.WaveLinks.Count,
                NoWavesLinked = n.WaveLinks.Count == 0,
                Waves = n.WaveLinks
                    .OrderBy(w => w.WaveType)
                    .Select(w => new WaveLinkDto { Id = w.Id, WaveType = w.WaveType.ToDisplay(), Reference = w.Reference, Notes = w.Notes })
                    .ToList(),
                AssignedResources = n.ResourceAssignments
                    .OrderBy(ra => ra.Role).ThenBy(ra => ra.Resource != null ? ra.Resource.Name : string.Empty)
                    .Select(ra => new NominationResourceDto(
                        ra.ResourceId,
                        ra.Resource != null ? ra.Resource.Name : string.Empty,
                        ra.Resource != null ? ra.Resource.Region : string.Empty,
                        ra.Role))
                    .ToList(),
                AssignedResourceCount = n.ResourceAssignments.Count
            };
        }).ToList();
    }

    public async Task<NominationDto?> UpdateAsync(int id, NominationUpdateDto input, CancellationToken ct = default)
    {
        var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return null;

        if (TryParseStatus(input.Status, out var status))
            n.Status = status;

        // Stage (Migration Status) is portal-editable as a manual override; FDO re-applies it on next import.
        if (!string.IsNullOrWhiteSpace(input.MigrationStatus))
            n.MigrationStatus = input.MigrationStatus.Trim();

        n.BlockedReason = ParseBlockerReason(input.BlockedReason);
        n.BlockedSince = input.BlockedSince;
        n.FollowUpDate = input.FollowUpDate;
        if (!string.IsNullOrWhiteSpace(input.Remarks))
            n.Remarks = input.Remarks.Trim();

        // Ownership (PM/CFTL) is portal-editable; edits are preserved across FDO imports.
        // Solution Architect is managed via resource assignment (AssignResourceAsync), not here.
        n.ProjectCoordinator = string.IsNullOrWhiteSpace(input.ProjectCoordinator) ? null : input.ProjectCoordinator.Trim();
        n.CftlPrimary = string.IsNullOrWhiteSpace(input.CftlPrimary) ? null : input.CftlPrimary.Trim();

        // Classification & velocity impact are portal-owned strategic governance.
        if (input.Classification is not null)
            n.Classification = string.IsNullOrWhiteSpace(input.Classification) ? null : input.Classification.Trim();
        if (input.VelocityImpact is not null)
            n.VelocityImpact = string.IsNullOrWhiteSpace(input.VelocityImpact) ? null : input.VelocityImpact.Trim();
        if (input.GhcpAdoptionLevel is not null)
            n.GhcpAdoptionLevel = input.GhcpAdoptionLevel is >= 0 and <= 7 ? input.GhcpAdoptionLevel : n.GhcpAdoptionLevel;
        if (input.ShortName is not null)
            n.ShortName = string.IsNullOrWhiteSpace(input.ShortName) ? null : input.ShortName.Trim();

        // Stamp a blocked-since date automatically when moving into a blocked/waiting state without one.
        if (IsBlockedState(n.Status) && n.BlockedSince is null)
            n.BlockedSince = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsBlockedState(n.Status))
            n.BlockedReason = null;

        n.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var list = await GetAsync(null, null, ct);
        return list.FirstOrDefault(x => x.Id == id);
    }

    public async Task<WaveLinkDto?> AddWaveLinkAsync(int nominationId, WaveLinkUpsertDto input, CancellationToken ct = default)
    {
        var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == nominationId, ct);
        if (n is null || string.IsNullOrWhiteSpace(input.Reference)) return null;

        var link = new WaveLink
        {
            NominationId = nominationId,
            AccountId = n.AccountId,
            WaveType = ParseWaveType(input.WaveType),
            Reference = input.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            Source = "Portal"
        };
        _db.WaveLinks.Add(link);
        await _db.SaveChangesAsync(ct);
        return new WaveLinkDto { Id = link.Id, WaveType = link.WaveType.ToDisplay(), Reference = link.Reference, Notes = link.Notes };
    }

    public async Task<bool> DeleteWaveLinkAsync(int nominationId, int waveLinkId, CancellationToken ct = default)
    {
        var link = await _db.WaveLinks.FirstOrDefaultAsync(w => w.Id == waveLinkId && w.NominationId == nominationId, ct);
        if (link is null) return false;
        _db.WaveLinks.Remove(link);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<NominationResourceDto?> AssignResourceAsync(int nominationId, AssignResourceDto input, CancellationToken ct = default)
    {
        var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == nominationId, ct);
        if (n is null) return null;
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.ResourceId == input.ResourceId, ct);
        if (resource is null) return null;

        var role = string.IsNullOrWhiteSpace(input.Role) ? null : input.Role.Trim();
        var link = await _db.NominationResources
            .FirstOrDefaultAsync(x => x.NominationId == nominationId && x.ResourceId == input.ResourceId, ct);
        if (link is null)
        {
            link = new NominationResource { NominationId = nominationId, ResourceId = input.ResourceId, Role = role, Source = "Portal" };
            _db.NominationResources.Add(link);
        }
        else
        {
            link.Role = role;
            link.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        // The Solution Architect assignment is the single source for the nomination's SA field (grid SA column).
        if (string.Equals(role, "Solution Architect", StringComparison.OrdinalIgnoreCase))
            n.SolutionArchitect = resource.Name;

        await _db.SaveChangesAsync(ct);
        return new NominationResourceDto(resource.ResourceId, resource.Name, resource.Region, role);
    }

    public async Task<bool> UnassignResourceAsync(int nominationId, int resourceId, CancellationToken ct = default)
    {
        var link = await _db.NominationResources
            .FirstOrDefaultAsync(x => x.NominationId == nominationId && x.ResourceId == resourceId, ct);
        if (link is null) return false;
        var wasSolutionArchitect = string.Equals(link.Role, "Solution Architect", StringComparison.OrdinalIgnoreCase);
        _db.NominationResources.Remove(link);
        await _db.SaveChangesAsync(ct);

        if (wasSolutionArchitect)
        {
            // Re-derive the nomination SA from any remaining SA assignment; else clear it.
            var remaining = await _db.NominationResources
                .Where(x => x.NominationId == nominationId && x.Role == "Solution Architect")
                .OrderBy(x => x.Id)
                .Select(x => x.Resource!.Name)
                .FirstOrDefaultAsync(ct);
            var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == nominationId, ct);
            if (n is not null)
            {
                n.SolutionArchitect = remaining;
                await _db.SaveChangesAsync(ct);
            }
        }
        return true;
    }

    private async Task<(int warn, int escalate, int defer)> GetStaleTiersAsync(CancellationToken ct)
    {
        var settings = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == "StaleWarnDays" || s.Key == "StaleEscalateDays" || s.Key == "StaleDeferDays")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        int Get(string k, int d) => settings.TryGetValue(k, out var v) && int.TryParse(v, out var n) ? n : d;
        return (Get("StaleWarnDays", 3), Get("StaleEscalateDays", 5), Get("StaleDeferDays", 10));
    }

    // Strategic-pilot time thresholds (A.13): 0–60 Green, 61–90 Amber, 91–120 Red, >120 Exec.
    private async Task<(int green, int amber, int red)> GetStrategicThresholdsAsync(CancellationToken ct)
    {
        var settings = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == "StrategicGreenDays" || s.Key == "StrategicAmberDays" || s.Key == "StrategicRedDays")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        int Get(string k, int d) => settings.TryGetValue(k, out var v) && int.TryParse(v, out var n) ? n : d;
        return (Get("StrategicGreenDays", 60), Get("StrategicAmberDays", 90), Get("StrategicRedDays", 120));
    }

    private static string StrategicTier(bool isStrategic, NominationStatusType status, int inFlightDays, int green, int amber, int red)
    {
        if (!isStrategic) return string.Empty;
        // Settled pilots are no longer actively governed against the running clock.
        if (status is NominationStatusType.Completed or NominationStatusType.Closed or NominationStatusType.CustomerDeferred or NominationStatusType.Withdrawn)
            return string.Empty;
        if (inFlightDays > red) return "Exec";
        if (inFlightDays > amber) return "Red";
        if (inFlightDays > green) return "Amber";
        return "Green";
    }

    // Migration Status text → numeric stage (1–4). The SLA cadence applies to execution stages 2–4.
    private static int? StageIndex(string? migration)
    {
        var m = migration?.ToLowerInvariant() ?? string.Empty;
        if (m.Contains("validating")) return 1;
        if (m.Contains("pre-requisite") || m.Contains("pre requisite") || m.Contains("prerequisite")) return 2;
        if (m.Contains("finalize")) return 3;
        if (m.Contains("executing migration")) return 4;
        return null;
    }

    private static string StaleTier(NominationStatusType status, int? stage, int days, int warn, int escalate, int defer)
    {
        // Documented cadence applies to execution stages 2–4 only (not initial validation/scoping).
        if (stage is null or < 2)
            return string.Empty;
        // Completed/closed/deferred/withdrawn items are settled — not chased.
        if (status is NominationStatusType.Completed or NominationStatusType.Closed or NominationStatusType.CustomerDeferred or NominationStatusType.Withdrawn)
            return string.Empty;
        if (days >= defer) return "Defer";
        if (days >= escalate) return "Escalate";
        if (days >= warn) return "Warn";
        return string.Empty;
    }

    private static bool IsBlockedState(NominationStatusType s) =>
        s is NominationStatusType.Blocked or NominationStatusType.WaitingForCustomerAction or NominationStatusType.WaitingOnFollowUp;

    private static bool TryParseStatus(string? value, out NominationStatusType status)
    {
        status = NominationStatusType.Open;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var key = value.Replace(" ", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse(key, true, out status);
    }

    private static BlockerReasonType? ParseBlockerReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var key = value.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse<BlockerReasonType>(key, true, out var r) && r != BlockerReasonType.None ? r : null;
    }

    private static WaveType ParseWaveType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return WaveType.App;
        var key = value.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("-", string.Empty);
        if (key.Equals("SecurityDefender", StringComparison.OrdinalIgnoreCase)) return WaveType.Security;
        return Enum.TryParse<WaveType>(key, true, out var w) ? w : WaveType.App;
    }
}
