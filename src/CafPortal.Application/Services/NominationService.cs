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

        var query = _db.Nominations.AsNoTracking().Include(n => n.WaveLinks).Include(n => n.Account).AsQueryable();
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(n => n.Region == region);
        if (!string.IsNullOrWhiteSpace(status) && TryParseStatus(status, out var parsed))
            query = query.Where(n => n.Status == parsed);

        var items = await query.OrderByDescending(n => n.OpenedDate).ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return items.Select(n =>
        {
            var lastTouch = n.UpdatedUtc ?? n.CreatedUtc;
            var updateDays = Math.Max(0, today.DayNumber - DateOnly.FromDateTime(lastTouch.UtcDateTime).DayNumber);
            // Age = days in the current migration stage when available; else fall back to update recency.
            var days = n.StageAgeDays ?? updateDays;
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
                Tpid = n.Account?.Tpid,
                Technology = n.Technology,
                Region = n.Region,
                Status = n.Status.ToDisplay(),
                OpenedDate = n.OpenedDate,
                Remarks = n.Remarks,
                MigrationStatus = n.MigrationStatus,
                StageAgeDays = n.StageAgeDays,
                CurrentState = n.CurrentState,
                SolutionArchitect = n.SolutionArchitect,
                CftlPrimary = n.CftlPrimary,
                ProjectCoordinator = n.ProjectCoordinator,
                BlockedReason = n.BlockedReason?.ToDisplay(),
                BlockedSince = n.BlockedSince,
                FollowUpDate = n.FollowUpDate,
                DaysSinceUpdate = days,
                StaleTier = StaleTier(n.Status, StageIndex(n.MigrationStatus), days, warn, escalate, defer),
                DbLinked = dbLinked,
                AlzLinked = alzLinked,
                SecurityLinked = securityLinked,
                WaveCount = n.WaveLinks.Count,
                NoWavesLinked = n.WaveLinks.Count == 0,
                Waves = n.WaveLinks
                    .OrderBy(w => w.WaveType)
                    .Select(w => new WaveLinkDto { Id = w.Id, WaveType = w.WaveType.ToDisplay(), Reference = w.Reference, Notes = w.Notes })
                    .ToList()
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

        // Ownership (PM/CFTL/SA) is portal-editable; edits are preserved across FDO imports.
        n.ProjectCoordinator = string.IsNullOrWhiteSpace(input.ProjectCoordinator) ? null : input.ProjectCoordinator.Trim();
        n.CftlPrimary = string.IsNullOrWhiteSpace(input.CftlPrimary) ? null : input.CftlPrimary.Trim();
        n.SolutionArchitect = string.IsNullOrWhiteSpace(input.SolutionArchitect) ? null : input.SolutionArchitect.Trim();

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

    private async Task<(int warn, int escalate, int defer)> GetStaleTiersAsync(CancellationToken ct)
    {
        var settings = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == "StaleWarnDays" || s.Key == "StaleEscalateDays" || s.Key == "StaleDeferDays")
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        int Get(string k, int d) => settings.TryGetValue(k, out var v) && int.TryParse(v, out var n) ? n : d;
        return (Get("StaleWarnDays", 3), Get("StaleEscalateDays", 5), Get("StaleDeferDays", 10));
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
