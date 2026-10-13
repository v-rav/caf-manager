namespace CafPortal.Application.Dtos;

/// <summary>One member of a name-consistency cluster, scored against the group's canonical name.</summary>
public record NameVariantDto(int Id, string Name, string? Tpid, double Score, int LinkedCount);

/// <summary>A cluster of likely-same names; CanonicalName is the preferred master (TPID-bearing / most complete).</summary>
public record NameSuggestionDto(
    int CanonicalId,
    string CanonicalName,
    string? CanonicalTpid,
    double MinScore,
    IReadOnlyList<NameVariantDto> Variants);

public record NameConsistencyReportDto(
    string Entity,
    double Threshold,
    int GroupCount,
    int VariantCount,
    IReadOnlyList<NameSuggestionDto> Groups);

/// <summary>Register variant names as aliases of a canonical account (writes AccountAlias + Account.Aliases).</summary>
public record ApplyAccountAliasRequest(string CanonicalName, IReadOnlyList<string> Aliases);

/// <summary>Append variant names onto a roster resource's alias list.</summary>
public record ApplyResourceAliasRequest(int ResourceId, IReadOnlyList<string> Aliases);

public record NameConsistencyApplyResultDto(int AliasesWritten, string Message);

/// <summary>A free-text performance-review name that fuzzy-matches a roster resource it doesn't exactly equal.</summary>
public record PerformanceNameSuggestionDto(
    string CurrentName, int ReviewCount, int ResourceId, string RosterName, string? Psid, double Score);

public record PerformanceNameReportDto(double Threshold, int Count, IReadOnlyList<PerformanceNameSuggestionDto> Suggestions);

public record ApplyPerformanceNameRequest(string CurrentName, string RosterName);
