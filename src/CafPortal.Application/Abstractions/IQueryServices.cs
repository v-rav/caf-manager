using CafPortal.Application.Dtos;

namespace CafPortal.Application.Abstractions;

public interface IDashboardService
{
    Task<ExecutiveDashboardDto> GetExecutiveAsync(string? region, CancellationToken ct = default);
}

public interface IResourceService
{
    Task<IReadOnlyList<ResourceDto>> GetAllAsync(ResourceQuery query, CancellationToken ct = default);
    Task<ResourceDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ResourceDetailDto> CreateAsync(ResourceUpsertDto input, CancellationToken ct = default);
    Task<ResourceDetailDto?> UpdateAsync(int id, ResourceUpsertDto input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Links an account to the resource, then recomputes capacity.</summary>
    Task<bool> AssignAccountAsync(int resourceId, AssignAccountDto input, CancellationToken ct = default);

    /// <summary>Unlinks an account from the resource, then recomputes capacity.</summary>
    Task<bool> UnassignAccountAsync(int resourceId, int accountId, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> GetAllAsync(string? search, string? region, CancellationToken ct = default);
    Task<AccountDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<AccountDetailDto> CreateAsync(AccountUpsertDto input, CancellationToken ct = default);
    Task<AccountDetailDto?> UpdateAsync(int id, AccountUpsertDto input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ICapacityService
{
    Task<IReadOnlyList<CapacityRowDto>> GetAsync(string? region, CancellationToken ct = default);
}

public interface IStrategicAccountService
{
    Task<IReadOnlyList<StrategicAccountDto>> GetAsync(string? region, CancellationToken ct = default);
}

public interface ILeaveService
{
    Task<LeaveWindowDto> GetWindowAsync(int windowDays, string? region, CancellationToken ct = default);

    /// <summary>Resources with active accounts who have leave inside the window (coverage clash risk).</summary>
    Task<IReadOnlyList<LeaveClashDto>> GetClashesAsync(string? region, CancellationToken ct = default);

    Task<LeaveDto?> CreateAsync(LeaveUpsertDto input, CancellationToken ct = default);
    Task<LeaveDto?> UpdateAsync(int id, LeaveUpsertDto input, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface INominationService
{
    Task<IReadOnlyList<NominationDto>> GetAsync(string? region, string? status, CancellationToken ct = default);

    Task<NominationDto?> UpdateAsync(int id, NominationUpdateDto input, CancellationToken ct = default);

    Task<WaveLinkDto?> AddWaveLinkAsync(int nominationId, WaveLinkUpsertDto input, CancellationToken ct = default);
    Task<bool> DeleteWaveLinkAsync(int nominationId, int waveLinkId, CancellationToken ct = default);

    Task<NominationResourceDto?> AssignResourceAsync(int nominationId, AssignResourceDto input, CancellationToken ct = default);
    Task<bool> UnassignResourceAsync(int nominationId, int resourceId, CancellationToken ct = default);
}

public interface IPerformanceReviewService
{
    /// <summary>Latest review per person, with trend vs their previous review and training flags.</summary>
    Task<IReadOnlyList<PerformanceReviewDto>> GetLatestAsync(string? region, CancellationToken ct = default);

    /// <summary>Full dated history for one person (oldest → newest).</summary>
    Task<IReadOnlyList<PerformanceReviewDto>> GetHistoryAsync(string personName, CancellationToken ct = default);

    /// <summary>Adds a new dated review snapshot.</summary>
    Task<PerformanceReviewDto> CreateAsync(PerformanceReviewUpsertDto input, CancellationToken ct = default);
}
