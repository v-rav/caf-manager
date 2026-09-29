using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities.Governance;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>
/// ACR recovery ledger. Baseline is frozen at flag time and survives FDO re-imports; reconciliation
/// (run after every import) books the realized delta once the nomination's ACR rises above baseline,
/// attributing recovered ACR to the system analysis. Portal-owned — imports never touch this table.
/// </summary>
public class AcrRecoveryService(IApplicationDbContext db, ICurrentUser currentUser) : IAcrRecoveryService
{
    private readonly IApplicationDbContext _db = db;
    private readonly ICurrentUser _currentUser = currentUser;

    private static bool IsOpen(string status) => status is "Flagged" or "Notified";

    public async Task<IReadOnlyList<AcrRecoveryDto>> GetAllAsync(string? region, CancellationToken ct = default)
    {
        var rows = await (from e in _db.AcrRecoveryEntries.AsNoTracking()
                          join n in _db.Nominations.AsNoTracking() on e.NominationId equals n.Id into nj
                          from n in nj.DefaultIfEmpty()
                          join a in _db.Accounts.AsNoTracking() on n.AccountId equals a.AccountId into aj
                          from a in aj.DefaultIfEmpty()
                          where string.IsNullOrEmpty(region) || (n != null && n.Region == region)
                          select new { e, Account = n != null ? n.AccountName : null, Tpid = a != null ? a.Tpid : null, Region = n != null ? n.Region : null, Current = n != null ? n.TotalAcr : null })
                          .ToListAsync(ct);

        // Open claims first (actionable), then realized, then closed; biggest baseline/recovered first.
        static int Rank(string s) => s switch { "Flagged" => 0, "Notified" => 0, "Realized" => 1, _ => 2 };
        return rows
            .OrderBy(x => Rank(x.e.Status))
            .ThenByDescending(x => x.e.RecoveredAcr ?? x.e.BaselineAcr ?? 0m)
            .Select(x => new AcrRecoveryDto(
                x.e.Id, x.e.NominationId, x.Account ?? "—", x.Tpid, x.Region, x.e.GapType,
                x.e.BaselineCores, x.e.BaselineAcr, x.e.RecommendedCores, x.e.RecommendedAcr,
                x.e.FlaggedUtc, x.e.FlaggedBy, x.e.NotifiedUtc, x.e.NotifiedBy,
                x.e.RealizedCores, x.e.RealizedAcr, x.e.RealizedUtc, x.e.RecoveredAcr,
                x.Current, x.e.Status))
            .ToList();
    }

    public async Task<AcrRecoverySummaryDto> GetSummaryAsync(string? region, CancellationToken ct = default)
    {
        var entries = await (from e in _db.AcrRecoveryEntries.AsNoTracking()
                             join n in _db.Nominations.AsNoTracking() on e.NominationId equals n.Id into nj
                             from n in nj.DefaultIfEmpty()
                             where string.IsNullOrEmpty(region) || (n != null && n.Region == region)
                             select e).ToListAsync(ct);

        var flagged = entries.Count;
        var notified = entries.Count(e => e.NotifiedUtc != null);
        var realized = entries.Where(e => e.Status == "Realized").ToList();
        var closed = entries.Count(e => e.Status == "Closed");
        var open = entries.Where(e => IsOpen(e.Status)).ToList();

        double? avgDays = realized.Any(e => e.RealizedUtc != null)
            ? realized.Where(e => e.RealizedUtc != null)
                .Average(e => (e.RealizedUtc!.Value - (e.NotifiedUtc ?? e.FlaggedUtc)).TotalDays)
            : null;
        var acc = realized.Where(e => (e.RecommendedAcr ?? 0m) > 0m && e.RealizedAcr != null).ToList();
        double? accuracy = acc.Any()
            ? (double)acc.Average(e => (e.RealizedAcr!.Value - (e.BaselineAcr ?? 0m)) / Math.Max(1m, (e.RecommendedAcr!.Value - (e.BaselineAcr ?? 0m)))) * 100
            : null;

        return new AcrRecoverySummaryDto(
            flagged, notified, realized.Count, closed,
            open.Sum(e => e.BaselineAcr ?? 0m),
            open.Sum(e => e.RecommendedAcr ?? 0m),
            realized.Sum(e => e.RecoveredAcr ?? 0m),
            flagged > 0 ? Math.Round((double)realized.Count / flagged * 100, 1) : 0,
            avgDays is null ? null : Math.Round(avgDays.Value, 1),
            accuracy is null ? null : Math.Round(accuracy.Value, 1));
    }

    public async Task<bool> FlagAsync(AcrRecoveryFlagRequest req, CancellationToken ct = default)
    {
        var n = await _db.Nominations.FirstOrDefaultAsync(x => x.Id == req.NominationId, ct);
        if (n is null) return false;
        // One open claim per nomination.
        var existing = await _db.AcrRecoveryEntries
            .FirstOrDefaultAsync(e => e.NominationId == req.NominationId && (e.Status == "Flagged" || e.Status == "Notified"), ct);
        if (existing != null) return true;

        _db.AcrRecoveryEntries.Add(new AcrRecoveryEntry
        {
            NominationId = req.NominationId,
            GapType = req.GapType,
            BaselineCores = n.TotalCores,
            BaselineAcr = n.TotalAcr,
            RecommendedCores = req.RecommendedCores,
            RecommendedAcr = req.RecommendedAcr,
            FlaggedUtc = DateTime.UtcNow,
            FlaggedBy = _currentUser.Name,
            Status = "Flagged",
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkNotifiedAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.AcrRecoveryEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null || !IsOpen(e.Status)) return false;
        e.NotifiedUtc = DateTime.UtcNow;
        e.NotifiedBy = _currentUser.Name;
        e.Status = "Notified";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CloseAsync(int id, string? note, CancellationToken ct = default)
    {
        var e = await _db.AcrRecoveryEntries.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return false;
        e.Status = "Closed";
        e.ClosedUtc = DateTime.UtcNow;
        e.Note = note;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> ReconcileAsync(CancellationToken ct = default)
    {
        var open = await _db.AcrRecoveryEntries
            .Where(e => e.Status == "Flagged" || e.Status == "Notified").ToListAsync(ct);
        if (open.Count == 0) return 0;

        var ids = open.Select(e => e.NominationId).Distinct().ToList();
        var noms = await _db.Nominations.AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .Select(n => new { n.Id, n.TotalAcr, n.TotalCores })
            .ToDictionaryAsync(n => n.Id, ct);

        var realized = 0;
        foreach (var e in open)
        {
            if (!noms.TryGetValue(e.NominationId, out var n)) continue;
            var baseline = e.BaselineAcr ?? 0m;
            var current = n.TotalAcr ?? 0m;
            if (current > baseline)
            {
                e.RealizedAcr = current;
                e.RealizedCores = n.TotalCores;
                e.RealizedUtc = DateTime.UtcNow;
                e.RecoveredAcr = current - baseline;
                e.Status = "Realized";
                realized++;
            }
        }
        if (realized > 0) await _db.SaveChangesAsync(ct);
        return realized;
    }
}
