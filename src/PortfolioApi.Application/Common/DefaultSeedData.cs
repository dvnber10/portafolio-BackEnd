namespace PortfolioApi.Application.Common;

public static class DefaultSeedData
{
    public static string SeedJson => Load();

    private static string Load()
    {
        var relative = Path.Combine(AppContext.BaseDirectory, "Data", "cv_data.json");
        if (File.Exists(relative))
            return File.ReadAllText(relative);

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "Data", "cv_data.json");
        if (File.Exists(fallback))
            return File.ReadAllText(fallback);

        return "{}";
    }
}