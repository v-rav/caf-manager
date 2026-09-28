namespace CafPortal.Application.Dtos;

/// <summary>A portal page and the roles allowed to see it (Admin always bypasses).</summary>
public record PageAccessDto(string Key, string Label, IReadOnlyList<string> AllowedRoles);

public record PageAccessUpdate(string Key, IReadOnlyList<string> AllowedRoles);
