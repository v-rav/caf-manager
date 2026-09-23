using CafPortal.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CafPortal.Infrastructure.Jobs;

/// <summary>Runs the data refresh once per day at the configured RefreshTime (default 02:00).</summary>
public class DailyRefreshHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyRefreshHostedService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<DailyRefreshHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await CalculateDelayAsync(stoppingToken);
            _logger.LogInformation("Next scheduled data refresh in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunRefreshAsync(stoppingToken);
        }
    }

    private async Task<TimeSpan> CalculateDelayAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsProvider>();
        var raw = await settings.GetStringAsync(SettingKeys.RefreshTime, "02:00", ct);
        if (!TimeOnly.TryParse(raw, out var refreshTime))
            refreshTime = new TimeOnly(2, 0);

        var now = DateTime.Now;
        var next = now.Date.Add(refreshTime.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    private async Task RunRefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var refresh = scope.ServiceProvider.GetRequiredService<IDataRefreshService>();
            var result = await refresh.RefreshAsync(ct);
            _logger.LogInformation("Scheduled refresh completed. Success={Success}, CapacityRows={Rows}",
                result.Success, result.CapacityRowsRebuilt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled refresh threw");
        }
    }
}
