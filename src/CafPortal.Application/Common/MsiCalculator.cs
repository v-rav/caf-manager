namespace CafPortal.Application.Common;

/// <summary>Migration Success Index — the single per-nomination health score (0–100).
/// 30% Readiness + 20% Scope + 20% Delivery + 10% Risk + 10% GHCP + 10% Sign-off.</summary>
public static class MsiCalculator
{
    public readonly record struct MsiResult(
        int Score, string Band, int Readiness, int Scope, int Delivery, int Risk, int Ghcp, int Signoff);

    public static MsiResult Compute(int readiness, int scope, int delivery, int risk, int ghcp, int signoff)
    {
        var score = (int)Math.Round(0.30 * readiness + 0.20 * scope + 0.20 * delivery + 0.10 * risk + 0.10 * ghcp + 0.10 * signoff);
        var band = score > 80 ? "Green" : score >= 60 ? "Amber" : "Red";
        return new MsiResult(score, band, readiness, scope, delivery, risk, ghcp, signoff);
    }

    /// <summary>Risk health from open blockers: full health with none, degraded per open blocker, worse if the clock is stopped.</summary>
    public static int RiskHealth(int openBlockers, bool anyClockStopped)
        => Math.Max(0, 100 - openBlockers * 30 - (anyClockStopped ? 20 : 0));

    public static int GhcpScore(int? adoptionLevel)
        => (int)Math.Round(Math.Clamp(adoptionLevel ?? 0, 0, 7) / 7.0 * 100);

    /// <summary>Which MSI component a gate key contributes to.</summary>
    public static string GroupFor(string gateKey) => gateKey switch
    {
        "G1" or "G2" or "G3" => "Readiness",
        "G4" or "G5" => "Scope",
        "G6" or "G7" => "Delivery",
        "G8" => "Signoff",
        _ => "Readiness",
    };
}
