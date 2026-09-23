using CafPortal.Application.Abstractions;
using CafPortal.Application.Common;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class NominationService(IApplicationDbContext db) : INominationService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<IReadOnlyList<NominationDto>> GetAsync(string? region, string? status, CancellationToken ct = default)
    {
        var query = _db.Nominations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(n => n.Region == region);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<NominationStatusType>(
                status.Replace(" ", string.Empty), true, out var parsed))
            query = query.Where(n => n.Status == parsed);

        var items = await query
            .OrderByDescending(n => n.OpenedDate)
            .Select(n => new
            {
                n.Id,
                n.AccountId,
                AccountName = n.AccountName ?? (n.Account != null ? n.Account.AccountName : null),
                n.Technology,
                n.Region,
                n.Status,
                n.OpenedDate,
                n.Remarks,
                n.MigrationStatus,
                n.CurrentState,
                n.SolutionArchitect,
                n.CftlPrimary,
                n.ProjectCoordinator
            })
            .ToListAsync(ct);

        return items.Select(n => new NominationDto
        {
            Id = n.Id,
            AccountId = n.AccountId,
            AccountName = n.AccountName,
            Technology = n.Technology,
            Region = n.Region,
            Status = n.Status.ToDisplay(),
            OpenedDate = n.OpenedDate,
            Remarks = n.Remarks,
            MigrationStatus = n.MigrationStatus,
            CurrentState = n.CurrentState,
            SolutionArchitect = n.SolutionArchitect,
            CftlPrimary = n.CftlPrimary,
            ProjectCoordinator = n.ProjectCoordinator
        }).ToList();
    }
}
