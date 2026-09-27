namespace CafPortal.Application.Abstractions;

using CafPortal.Application.Dtos;

/// <summary>Reads/writes the editable ACR rate master and estimates ACR from app/core counts.</summary>
public interface IAcrService
{
    Task<AcrRatesDto> GetRatesAsync(CancellationToken ct = default);
    Task<AcrRatesDto> SaveRatesAsync(AcrRatesDto rates, CancellationToken ct = default);
    Task<AcrEstimateResult> EstimateAsync(AcrEstimateRequest request, CancellationToken ct = default);
}
