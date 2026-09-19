using PortfolioApi.Application.Models;

namespace PortfolioApi.Application.Dtos;

public record ProfileSummaryDto(
    string Slug,
    string Title,
    string ShortTitle,
    string Icon,
    int Order,
    bool IsDefault,
    bool IsGeneral,
    int ProjectCount);

public record PortfolioSummaryDto(
    PersonalInfo Personal,
    string DefaultSlug,
    IReadOnlyList<ProfileSummaryDto> Profiles);

public record ProjectDto(
    Guid Id,
    string Title,
    string Category,
    string Description,
    string Link,
    string? RepoLink,
    string? ImageUrl,
    int Order);

public record ProfilePageDto(
    ProfileSummaryDto Profile,
    IReadOnlyList<ProjectDto> Projects,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Skills,
    IReadOnlyList<ExperienceItem> Experience,
    IReadOnlyList<EducationItem> Education,
    IReadOnlyList<LanguageItem> Languages);

public record CvDocumentDto(string Json, DateTime UpdatedAt);

public record SaveCvResultDto(bool Ok, string? Error, int ProfileCount, int ProjectCount);

public record VisitStatsDto(long Visits);