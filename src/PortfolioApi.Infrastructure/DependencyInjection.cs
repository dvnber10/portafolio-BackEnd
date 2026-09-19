using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioApi.Application.Common;
using PortfolioApi.Application.Services;
using PortfolioApi.Infrastructure.Common;
using PortfolioApi.Infrastructure.Pdf;
using PortfolioApi.Infrastructure.Persistence;

namespace PortfolioApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration["Database:Connection"]
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=PortfolioApi;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";

        services.AddDbContext<PortfolioDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<IPortfolioDbContext>(sp => sp.GetRequiredService<PortfolioDbContext>());

        services.AddScoped<IAdminKeyValidator, AdminKeyValidator>();
        services.AddScoped<ICvPdfRenderer, CvPdfRenderer>();

        return services;
    }
}