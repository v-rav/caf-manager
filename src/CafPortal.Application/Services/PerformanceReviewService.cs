using CafPortal.Application.Abstractions;
using CafPortal.Application.Dtos;
using CafPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafPortal.Application.Services;

public class PerformanceReviewService(IApplicationDbContext db) : IPerformanceReviewService
{
    private readonly IApplicationDbContext _db = db;

    public async Task<IReadOnlyList<PerformanceReviewDto>> GetLatestAsync(string? region, CancellationToken ct = default)
    {
        var threshold = await GetThresholdAsync(ct);

        var query = _db.PerformanceReviews.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(r => r.Region == region);

        var all = await query.ToListAsync(ct);

        // Latest snapshot per person, with the immediately-previous one for the trend delta.
        return all
            .GroupBy(r => r.PersonName)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(r => r.ReviewDate).ThenByDescending(r => r.Id).ToList();
                var latest = ordered[0];
                var previous = ordered.Skip(1).FirstOrDefault(r => Score(r) is not null);
                var dto = Map(latest, threshold);
                dto.ReviewCount = ordered.Count;
                dto.PreviousScore = previous is null ? null : Score(previous);
                dto.TrendDelta = dto.Score is not null && dto.PreviousScore is not null
                    ? Math.Round(dto.Score.Value - dto.PreviousScore.Value, 2)
                    : null;
                return dto;
            })
            .OrderByDescending(d => d.Pending ? -1 : d.Score ?? 0)
            .ThenBy(d => d.PersonName)
            .ToList();
    }

    public async Task<IReadOnlyList<PerformanceReviewDto>> GetHistoryAsync(string personName, CancellationToken ct = default)
    {
        var threshold = await GetThresholdAsync(ct);
        var items = await _db.PerformanceReviews.AsNoTracking()
            .Where(r => r.PersonName == personName)
            .ToListAsync(ct);
        return items
            .OrderBy(r => r.ReviewDate).ThenBy(r => r.Id)
            .Select(r => Map(r, threshold))
            .ToList();
    }

    public async Task<PerformanceReviewDto> CreateAsync(PerformanceReviewUpsertDto input, CancellationToken ct = default)
    {
        var review = new PerformanceReview
        {
            PersonName = input.PersonName.Trim(),
            Role = Trim(input.Role),
            ReportingManager = Trim(input.ReportingManager),
            Region = string.IsNullOrWhiteSpace(input.Region) ? "EMEA" : input.Region.Trim(),
            ReviewDate = input.ReviewDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            CommunicationVerbal = input.CommunicationVerbal,
            CommunicationWritten = input.CommunicationWritten,
            Attitude = input.Attitude,
            ProcessUnderstanding = input.ProcessUnderstanding,
            OfferingUnderstanding = input.OfferingUnderstanding,
            Comments = Trim(input.Comments)
        };
        _db.PerformanceReviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return Map(review, await GetThresholdAsync(ct));
    }

    private async Task<double> GetThresholdAsync(CancellationToken ct)
    {
        var v = await _db.ApplicationSettings.AsNoTracking()
            .Where(s => s.Key == "PerformanceTrainingThreshold")
            .Select(s => s.Value).FirstOrDefaultAsync(ct);
        return double.TryParse(v, out var t) ? t : 4;
    }

    private static double? Score(PerformanceReview r)
    {
        var vals = new[] { r.CommunicationVerbal, r.CommunicationWritten, r.Attitude, r.ProcessUnderstanding, r.OfferingUnderstanding }
            .Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return vals.Count == 0 ? null : Math.Round(vals.Average(), 2);
    }

    private static PerformanceReviewDto Map(PerformanceReview r, double threshold)
    {
        var score = Score(r);
        var needs = new List<string>();
        void Flag(double? v, string area) { if (v.HasValue && v.Value < threshold) needs.Add(area); }
        Flag(r.CommunicationVerbal, "Communication (verbal)");
        Flag(r.CommunicationWritten, "Communication (written)");
        Flag(r.Attitude, "Coaching / attitude");
        Flag(r.ProcessUnderstanding, "Process training");
        Flag(r.OfferingUnderstanding, "Offering / technical training");

        return new PerformanceReviewDto
        {
            Id = r.Id,
            PersonName = r.PersonName,
            Role = r.Role,
            ReportingManager = r.ReportingManager,
            Region = r.Region,
            ReviewDate = r.ReviewDate,
            CommunicationVerbal = r.CommunicationVerbal,
            CommunicationWritten = r.CommunicationWritten,
            Attitude = r.Attitude,
            ProcessUnderstanding = r.ProcessUnderstanding,
            OfferingUnderstanding = r.OfferingUnderstanding,
            Score = score,
            Pending = score is null,
            TrainingNeeds = needs,
            ReviewCount = 1,
            Comments = r.Comments
        };
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
