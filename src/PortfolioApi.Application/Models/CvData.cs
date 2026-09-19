using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortfolioApi.Application.Models;

public class PersonalInfo
{
    [JsonPropertyName("fullName")] public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("location")] public string Location { get; set; } = string.Empty;
    [JsonPropertyName("phone")] public string Phone { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("github")] public string Github { get; set; } = string.Empty;
    [JsonPropertyName("linkedin")] public string Linkedin { get; set; } = string.Empty;
    [JsonPropertyName("portfolio")] public string Portfolio { get; set; } = string.Empty;
    [JsonPropertyName("photoUrl")] public string? PhotoUrl { get; set; }
}

public class EducationItem
{
    [JsonPropertyName("period")] public string Period { get; set; } = string.Empty;
    [JsonPropertyName("degree")] public string Degree { get; set; } = string.Empty;
    [JsonPropertyName("institution")] public string Institution { get; set; } = string.Empty;
    [JsonPropertyName("place")] public string Place { get; set; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; set; } = string.Empty;
}

public class ExperienceItem
{
    [JsonPropertyName("period")] public string Period { get; set; } = string.Empty;
    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
    [JsonPropertyName("organization")] public string Organization { get; set; } = string.Empty;
    [JsonPropertyName("location")] public string Location { get; set; } = string.Empty;
    [JsonPropertyName("project")] public string? Project { get; set; }
    [JsonPropertyName("link")] public string? Link { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "professional";
    [JsonPropertyName("responsibilities")] public List<string> Responsibilities { get; set; } = new();
    [JsonPropertyName("profiles")] public List<string> Profiles { get; set; } = new();
}

public class LanguageItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("level")] public string Level { get; set; } = string.Empty;
}

public class ProfileItem
{
    [JsonPropertyName("slug")] public string Slug { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("shortTitle")] public string ShortTitle { get; set; } = string.Empty;
    [JsonPropertyName("icon")] public string Icon { get; set; } = "💼";
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("default")] public bool IsDefault { get; set; }
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("highlights")] public List<string> Highlights { get; set; } = new();
    [JsonPropertyName("skills")] public List<string> Skills { get; set; } = new();
    [JsonPropertyName("interests")] public string Interests { get; set; } = string.Empty;
}

public class ProjectItem
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; set; } = "Desarrollo";
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;
    [JsonPropertyName("repoLink")] public string? RepoLink { get; set; }
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("profiles")] public List<string> Profiles { get; set; } = new();
}

public class CvData
{
    [JsonPropertyName("personal")] public PersonalInfo Personal { get; set; } = new();
    [JsonPropertyName("education")] public List<EducationItem> Education { get; set; } = new();
    [JsonPropertyName("experience")] public List<ExperienceItem> Experience { get; set; } = new();
    [JsonPropertyName("languages")] public List<LanguageItem> Languages { get; set; } = new();
    [JsonPropertyName("profiles")] public List<ProfileItem> Profiles { get; set; } = new();
    [JsonPropertyName("projects")] public List<ProjectItem> Projects { get; set; } = new();
}

public static class CvDataParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static CvData Parse(string json)
    {
        var data = JsonSerializer.Deserialize<CvData>(json, Options) ?? new CvData();
        data.Personal ??= new PersonalInfo();
        data.Education ??= new List<EducationItem>();
        data.Experience ??= new List<ExperienceItem>();
        data.Languages ??= new List<LanguageItem>();
        data.Profiles ??= new List<ProfileItem>();
        data.Projects ??= new List<ProjectItem>();
        return data;
    }

    public static string Serialize(CvData data)
        => JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
}