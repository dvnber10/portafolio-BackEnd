using Microsoft.EntityFrameworkCore;
using PortfolioApi.Application.Common;
using PortfolioApi.Application.Dtos;
using PortfolioApi.Application.Models;
using PortfolioApi.Application.Services;
using PortfolioApi.Domain.Entities;
using System.Text.Json;

namespace PortfolioApi.Application;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioDbContext _db;
    private readonly ICvPdfRenderer _pdfRenderer;

    public PortfolioService(IPortfolioDbContext db, ICvPdfRenderer pdfRenderer)
    {
        _db = db;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(CancellationToken ct = default)
    {
        var data = await LoadCvDataAsync(ct);
        return BuildSummary(data);
    }

    public async Task<ProfilePageDto?> GetProfilePageAsync(string slug, CancellationToken ct = default)
    {
        var data = await LoadCvDataAsync(ct);
        var profile = data.Profiles
            .OrderBy(p => p.Order)
            .FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (profile is null) return null;

        bool isGeneral = string.Equals(profile.Slug, "general", StringComparison.OrdinalIgnoreCase);
        var projects = data.Projects
            .Where(p => p.Profiles.Contains(profile.Slug))
            .OrderBy(p => p.Order)
            .Select(p => new ProjectDto(p.Id, p.Title, p.Category, p.Description, p.Link, p.RepoLink, p.ImageUrl, p.Order))
            .ToList();

        var summary = new ProfileSummaryDto(
            profile.Slug, profile.Title, profile.ShortTitle, profile.Icon, profile.Order,
            profile.IsDefault, isGeneral, projects.Count);

        return new ProfilePageDto(
            summary,
            projects,
            profile.Highlights,
            profile.Skills,
            data.Experience,
            data.Education,
            data.Languages);
    }

    public async Task<CvDocumentDto> GetCvDocumentAsync(CancellationToken ct = default)
    {
        var row = await GetOrCreateRowAsync(ct);
        return new CvDocumentDto(row.DataJson, row.UpdatedAt);
    }

    public async Task<SaveCvResultDto> SaveCvDocumentAsync(string json, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SaveCvResultDto(false, "El JSON no puede estar vacío.", 0, 0);

        CvData parsed;
        try
        {
            parsed = CvDataParser.Parse(json);
        }
        catch (JsonException)
        {
            return new SaveCvResultDto(false, "El JSON no es válido.", 0, 0);
        }

        if (parsed.Profiles.Count == 0)
            return new SaveCvResultDto(false, "Debe existir al menos un perfil.", 0, 0);

        var stored = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });

        var row = await GetOrCreateRowAsync(ct);
        row.DataJson = stored;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new SaveCvResultDto(true, null, parsed.Profiles.Count, parsed.Projects.Count);
    }

    public async Task<(byte[] Pdf, string FileName)> RenderProfilePdfAsync(string slug, CancellationToken ct = default)
    {
        var data = await LoadCvDataAsync(ct);
        var profile = data.Profiles.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase))
            ?? data.Profiles.FirstOrDefault()
            ?? throw new NotFoundException("No existe ningún perfil para generar el CV.");

        var pdf = _pdfRenderer.Render(data, profile.Slug);
        var safe = string.Concat(profile.Slug.Where(char.IsLetterOrDigit)).ToLowerInvariant();
        return (pdf, $"CV_EdgarBernal_{safe}.pdf");
    }

    public async Task<long> GetVisitsAsync(CancellationToken ct = default)
    {
        var row = await _db.SiteStats.AsNoTracking().FirstOrDefaultAsync(ct);
        return row?.VisitCount ?? 0;
    }

    public async Task<long> RegisterVisitAsync(CancellationToken ct = default)
    {
        var row = await _db.SiteStats.FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new SiteStats
            {
                Id = new Guid(SiteStats.SingletonId),
                VisitCount = 1
            };
            _db.SiteStats.Add(row);
        }
        else
        {
            row.VisitCount += 1;
        }
        await _db.SaveChangesAsync(ct);
        return row.VisitCount;
    }

    private async Task<CvData> LoadCvDataAsync(CancellationToken ct)
    {
        var row = await _db.GeneralCvData.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? await GetOrCreateRowAsync(ct);
        return CvDataParser.Parse(row.DataJson);
    }

    private async Task<GeneralCvData> GetOrCreateRowAsync(CancellationToken ct)
    {
        var row = await _db.GeneralCvData.FirstOrDefaultAsync(ct);
        if (row is not null) return row;

        row = new GeneralCvData
        {
            Id = new Guid(GeneralCvData.SingletonId),
            DataJson = DefaultSeedData.SeedJson,
            UpdatedAt = DateTime.UtcNow
        };
        _db.GeneralCvData.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private static PortfolioSummaryDto BuildSummary(CvData data)
    {
        var defaultProfile = data.Profiles.FirstOrDefault(p => p.IsDefault)
            ?? data.Profiles.FirstOrDefault();
        var summaries = data.Profiles
            .OrderBy(p => p.Order)
            .Select(p =>
            {
                var count = data.Projects.Count(pr => pr.Profiles.Contains(p.Slug));
                return new ProfileSummaryDto(
                    p.Slug, p.Title, p.ShortTitle, p.Icon, p.Order, p.IsDefault,
                    string.Equals(p.Slug, "general", StringComparison.OrdinalIgnoreCase), count);
            })
            .ToList();

        return new PortfolioSummaryDto(data.Personal, defaultProfile?.Slug ?? string.Empty, summaries);
    }
}