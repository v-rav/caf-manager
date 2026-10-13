using System.Text.RegularExpressions;

namespace CafPortal.Application.Common;

/// <summary>Aligns the free-text "Tool Name" from the FDO "Nomination Wize Tool Usage" export to the canonical
/// MigrationTool master name. The raw export conflates tool + activity (e.g. "GHCP-IaC" = GHCP doing IaC), so many
/// variants collapse to one master tool. Unknown names pass through trimmed (treated as already-canonical).</summary>
public static class ToolNameNormalizer
{
    // Raw variant (normalized: trimmed, lowercased, whitespace-collapsed) → canonical master tool name.
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ghcp"] = "GHCP-CI",
        ["ghcp-ci"] = "GHCP-CI",
        ["ghcp - ci"] = "GHCP-CI",
        ["ghcp ci"] = "GHCP-CI",
        ["ghcp ci skills"] = "GHCP-CI",
        ["ghcp-iac"] = "GHCP-CI",
        ["ghcp - iac"] = "GHCP-CI",
        ["ghcp iac"] = "GHCP-CI",
        ["ghcp cli"] = "AppMod CLI",
        ["ghcp app mod"] = "AppMod",
        ["ghcp app modernization"] = "AppMod",
        ["appcat analysis"] = "AppCAT",
        ["appcat"] = "AppCAT",
    };

    public static string? Canonical(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var key = Regex.Replace(raw.Trim().ToLowerInvariant(), @"\s+", " ");
        return Map.TryGetValue(key, out var canonical) ? canonical : raw.Trim();
    }
}
