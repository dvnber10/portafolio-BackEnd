using PortfolioApi.Application.Dtos;

namespace PortfolioApi.Application.Services;

public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken ct = default);
    Task<ProfilePageDto?> GetProfilePageAsync(string slug, CancellationToken ct = default);

    Task<CvDocumentDto> GetCvDocumentAsync(CancellationToken ct = default);
    Task<SaveCvResultDto> SaveCvDocumentAsync(string json, CancellationToken ct = default);

    Task<long> GetVisitsAsync(CancellationToken ct = default);
    Task<long> RegisterVisitAsync(CancellationToken ct = default);

    Task<(byte[] Pdf, string FileName)> RenderProfilePdfAsync(string slug, CancellationToken ct = default);
}