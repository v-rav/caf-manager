namespace CafPortal.Application.Common;

/// <summary>Aligns the free-text "Tool Name" from the FDO "Nomination Wize Tool Usage" export to the canonical
/// MigrationTool master name via KeywordRules "ToolAlias" (edited on the Classification Rules page).
/// Unmapped names pass through trimmed.</summary>
public static class ToolNameNormalizer
{
    public static string? Canonical(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return KeywordRules.First(KeywordRules.ToolAlias, raw) ?? raw.Trim();
    }
}
