namespace CafPortal.Application.Common;

/// <summary>MSI configuration, read from the settings catalog (Configuration → Scoring models).</summary>
public sealed record MsiModel(
    double Readiness, double Scope, double Delivery, double Risk, double Ghcp, double Signoff,
    double GreenAbove, double AmberFrom, double RiskPerBlocker, double RiskClockStop)
{
    public static MsiModel From(IReadOnlyDictionary<string, double> s) => new(
        s["MsiWeightReadiness"], s["MsiWeightScope"], s["MsiWeightDelivery"], s["MsiWeightRisk"], s["MsiWeightGhcp"],
        s["MsiWeightSignoff"], s["MsiGreenAbove"], s["MsiAmberFrom"], s["MsiRiskPerBlocker"], s["MsiRiskClockStop"]);
}

/// <summary>Migration Success Index — the single per-nomination health score (0–100). Weights, bands and risk
/// penalties come from <see cref="MsiModel"/>; gate → component mapping comes from <c>GateDefinition.MsiGroup</c>.</summary>
public static class MsiCalculator
{
    /// <summary>Gate-derived MSI components a gate can feed (Risk and GHCP are not gate-derived).</summary>
    public static readonly string[] GateGroups = { "Readiness", "Scope", "Delivery", "Signoff" };

    public static string NormalizeGroup(string? group) =>
        GateGroups.FirstOrDefault(g => g.Equals(group?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? GateGroups[0];

    public readonly record struct MsiResult(
        int Score, string Band, int Readiness, int Scope, int Delivery, int Risk, int Ghcp, int Signoff);

    public static MsiResult Compute(MsiModel m, int readiness, int scope, int delivery, int risk, int ghcp, int signoff)
    {
        var score = (int)Math.Round((m.Readiness * readiness + m.Scope * scope + m.Delivery * delivery
            + m.Risk * risk + m.Ghcp * ghcp + m.Signoff * signoff) / 100.0);
        var band = score > m.GreenAbove ? "Green" : score >= m.AmberFrom ? "Amber" : "Red";
        return new MsiResult(score, band, readiness, scope, delivery, risk, ghcp, signoff);
    }

    /// <summary>Risk health from open blockers: full health with none, degraded per open blocker, worse if the clock is stopped.</summary>
    public static int RiskHealth(MsiModel m, int openBlockers, bool anyClockStopped)
        => (int)Math.Max(0, 100 - openBlockers * m.RiskPerBlocker - (anyClockStopped ? m.RiskClockStop : 0));

    public static int GhcpScore(int? adoptionLevel)
        => (int)Math.Round(Math.Clamp(adoptionLevel ?? 0, 0, 7) / 7.0 * 100);
}
