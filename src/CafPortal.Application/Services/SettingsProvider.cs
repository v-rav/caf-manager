using CafPortal.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

/// <summary>Reads typed values from the ApplicationSettings table, falling back to sensible defaults.</summary>
public class SettingsProvider(IApplicationDbContext db) : ISettingsProvider
{
    private readonly IApplicationDbContext _db = db;

    public async Task<string> GetStringAsync(string key, string defaultValue, CancellationToken ct = default)
    {
        var setting = await _db.ApplicationSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);
        return string.IsNullOrWhiteSpace(setting?.Value) ? defaultValue : setting!.Value;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, defaultValue.ToString(), ct);
        return int.TryParse(raw, out var value) ? value : defaultValue;
    }

    public async Task<double> GetDoubleAsync(string key, double defaultValue, CancellationToken ct = default)
    {
        var raw = await GetStringAsync(key, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture), ct);
        return double.TryParse(raw, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
    }
}
