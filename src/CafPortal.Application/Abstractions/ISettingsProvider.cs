namespace CafPortal.Application.Abstractions;

/// <summary>Resolves values from the ApplicationSettings configuration table with typed fallbacks.</summary>
public interface ISettingsProvider
{
    Task<string> GetStringAsync(string key, string defaultValue, CancellationToken ct = default);
    Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default);
    Task<double> GetDoubleAsync(string key, double defaultValue, CancellationToken ct = default);
}

/// <summary>Well-known application setting keys. Values live in the ApplicationSettings table.</summary>
public static class SettingKeys
{
    public const string DefaultCapacityLimit = "DefaultCapacityLimit";
    public const string RefreshTime = "RefreshTime";
    public const string AvailableThreshold = "AvailableThreshold";
    public const string PartiallyUtilizedThreshold = "PartiallyUtilizedThreshold";
    public const string OverloadedThreshold = "OverloadedThreshold";
}
