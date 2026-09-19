using Microsoft.Extensions.DependencyInjection;
using PortfolioApi.Application.Services;

namespace PortfolioApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPortfolioService, PortfolioService>();
        return services;
    }
}