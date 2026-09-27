using CafPortal.Application.Abstractions;
using CafPortal.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CafPortal.Application;

public static class DependencyInjection
{
    /// <summary>Registers application-layer query services and the capacity engine.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISettingsProvider, SettingsProvider>();
        services.AddScoped<ICapacityCalculationService, CapacityCalculationService>();
        services.AddScoped<ICapacityRebuildService, CapacityRebuildService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICapacityService, CapacityService>();
        services.AddScoped<IStrategicAccountService, StrategicAccountService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<INominationService, NominationService>();
        services.AddScoped<IPerformanceReviewService, PerformanceReviewService>();
        services.AddScoped<IImportHistoryService, ImportHistoryService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGovernanceService, GovernanceService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IAcrService, AcrService>();
        return services;
    }
}
