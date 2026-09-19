using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PortfolioApi.Infrastructure.Persistence;

public class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = config["Database:Connection"]
            ?? config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=PortfolioApi;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new PortfolioDbContext(options);
    }
}