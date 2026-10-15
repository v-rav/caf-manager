using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Enums;

namespace CafPortal.Application.Services;

/// <summary>Ownership Effectiveness scorecard (PM / SA / CFTL), computed server-side from the nomination DTOs and the
/// Scoring-models settings. PM/CFTL are flow-led (on-track · momentum · speed · delivered share); SA is quality-led
/// (MSI · on-track · speed · momentum). Weights renormalise over the components that have data.</summary>
public class EffectivenessService(INominationService nominations, IApplicationDbContext db) : IEffectivenessService
{
    private static readonly string Completed = NominationStatusType.Completed.ToDisplay();
    private static readonly string Withdrawn = NominationStatusType.Withdrawn.ToDisplay();
    private static readonly string Deferred = NominationStatusType.CustomerDeferred.ToDisplay();
    private static readonly string Closed = NominationStatusType.Closed.ToDisplay();
    private static readonly string Blocked = NominationStatusType.Blocked.ToDisplay();

    public static readonly string[] Roles = { "PM", "SA", "CFTL" };

    public static string? OwnerOf(NominationDto n, string role)
    {
        var v = role switch { "SA" => n.SolutionArchitect, "CFTL" => n.CftlPrimary, _ => n.ProjectCoordinator };
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    public async Task<EffectivenessDto> GetAsync(string? region, string role, int? fy, CancellationToken ct = default)
    {
        role = Roles.FirstOrDefault(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)) ?? "PM";
        var m = await SettingsCatalog.ReadAsync(db, ct);
        // FY + region scoped; the synthetic "Closed" status is excluded (ambiguous, not a real outcome).
        var scoped = (await nominations.GetAsync(region, null, ct))
            .Where(n => (fy is null || n.FiscalYear == fy) && n.Status != Closed)
            .ToList();

        var rows = scoped.Select(n => (Owner: OwnerOf(n, role), N: n)).Where(x => x.Owner != null)
            .GroupBy(x => x.Owner!)
            .Select(g => Score(g.Key, g.Select(x => x.N).ToList(), role, m))
            .OrderByDescending(r => r.Effectiveness).ToList();

        var roleNoms = scoped.Where(n => OwnerOf(n, role) != null).ToList();
        var delivered = roleNoms.Where(n => n.Status == Completed).ToList();
        var active = roleNoms.Where(n => !n.IsSettled).ToList();
        var durs = delivered.Where(n => !n.IsStrategic && n.DeliveryDays != null).Select(n => n.DeliveryDays!.Value).ToList();
        return new EffectivenessDto
        {
            Role = role,
            GreenFrom = m["EffGreenFrom"],
            AmberFrom = m["EffAmberFrom"],
            Owners = rows.Count,
            Assigned = roleNoms.Count,
            Delivered = delivered.Count,
            Active = active.Count,
            DeliveredPct = roleNoms.Count > 0 ? R(100.0 * delivered.Count / roleNoms.Count) : 0,
            AvgDeliveryDays = durs.Count > 0 ? R(durs.Average()) : null,
            AvgActiveAge = active.Any(n => !n.IsStrategic) ? R(active.Where(n => !n.IsStrategic).Average(n => n.EffectiveAgeDays)) : null,
            Rows = rows,
        };
    }

    private static EffectivenessRowDto Score(string name, List<NominationDto> list, string role, IReadOnlyDictionary<string, double> m)
    {
        var assigned = list.Count;
        var activeList = list.Where(n => !n.IsSettled).ToList();
        var active = activeList.Count;
        var completed = list.Where(n => n.Status == Completed).ToList();
        var delivered = completed.Count;
        var withdrawn = list.Count(n => n.Status == Withdrawn);
        var deferred = list.Count(n => n.Status == Deferred);
        var blocked = list.Count(n => n.OpenBlockerCount > 0 || n.Status == Blocked);
        var settled = delivered + withdrawn + deferred;
        var deliveredPct = assigned > 0 ? R(100.0 * delivered / assigned) : 0;
        // Velocity (speed, momentum, on-track) is Standard-only; strategic work still counts in assigned/delivered.
        var velocityActive = activeList.Where(n => !n.IsStrategic).ToList();
        var durs = completed.Where(n => !n.IsStrategic && n.DeliveryDays != null).Select(n => n.DeliveryDays!.Value).ToList();
        int? avgDeliveryDays = durs.Count > 0 ? R(durs.Average()) : null;
        int? avgActiveAge = velocityActive.Count > 0 ? R(velocityActive.Average(n => n.EffectiveAgeDays)) : null;
        var avgMsi = R(list.Average(n => n.MsiScore));
        int? onTrackPct = velocityActive.Count > 0
            ? R(100.0 * velocityActive.Count(n => string.IsNullOrEmpty(n.StaleTier)) / velocityActive.Count) : null;
        var slaBreaches = activeList.Count(n => !string.IsNullOrEmpty(n.StaleTier));
        var ghcps = list.Select(n => (double)n.GhcpEffectiveLevel).ToList();
        double? avgGhcp = ghcps.Count > 0 ? Math.Round(ghcps.Average(), 1, MidpointRounding.AwayFromZero) : null;

        var sp = Linear(avgDeliveryDays, m["EffSpeedBestDays"], m["EffSpeedWorstDays"]);
        var mo = Linear(avgActiveAge, m["EffMomentumBestDays"], m["EffMomentumWorstDays"]);
        var parts = new List<(string Label, double W, double V)>();
        if (role == "SA")
        {
            parts.Add(($"MSI {avgMsi}", m["EffSaMsi"], avgMsi));
            if (onTrackPct is int ot) parts.Add(($"on-track {ot}%", m["EffSaOnTrack"], ot));
            if (sp is int s) parts.Add(($"speed {s}", m["EffSaSpeed"], s));
            if (mo is int mm) parts.Add(($"momentum {mm}", m["EffSaMomentum"], mm));
        }
        else
        {
            if (onTrackPct is int ot) parts.Add(($"on-track {ot}%", m["EffFlowOnTrack"], ot));
            if (mo is int mm) parts.Add(($"momentum {mm}", m["EffFlowMomentum"], mm));
            if (sp is int s) parts.Add(($"speed {s}", m["EffFlowSpeed"], s));
            parts.Add(($"delivered {deliveredPct}%", m["EffFlowDelivered"], deliveredPct));
        }
        var wsum = parts.Sum(p => p.W);
        if (wsum == 0) wsum = 1;
        var effectiveness = R(parts.Sum(p => p.W * p.V) / wsum);
        var tone = effectiveness >= m["EffGreenFrom"] ? "success" : effectiveness >= m["EffAmberFrom"] ? "warning" : "danger";

        return new EffectivenessRowDto
        {
            Name = name, Assigned = assigned, Active = active, Delivered = delivered, Withdrawn = withdrawn,
            Deferred = deferred, Blocked = blocked, SuccessRate = settled > 0 ? R(100.0 * delivered / settled) : null,
            DeliveredPct = deliveredPct, AvgDeliveryDays = avgDeliveryDays, AvgActiveAge = avgActiveAge, AvgMsi = avgMsi,
            OnTrackPct = onTrackPct, SlaBreaches = slaBreaches, AvgGhcp = avgGhcp,
            AcrDelivered = (double)completed.Sum(n => n.TotalAcr ?? 0m),
            Effectiveness = effectiveness, Tone = tone,
            Explain = $"{(role == "SA" ? "Quality-led" : "Flow-led")}: {string.Join(" · ", parts.Select(p => p.Label))} (weighted, renormalised)",
        };
    }

    private static int R(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

    // 100 at/under `best` days, 0 at/over `worst`.
    private static int? Linear(int? days, double best, double worst) =>
        days is null ? null : Math.Clamp(R((worst - days.Value) / (worst - best) * 100), 0, 100);
}
