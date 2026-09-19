using Microsoft.Extensions.Configuration;
using PortfolioApi.Application.Common;

namespace PortfolioApi.Infrastructure.Common;

public class AdminKeyValidator : IAdminKeyValidator
{
    private readonly string? _configuredKey;

    public AdminKeyValidator(IConfiguration configuration)
    {
        _configuredKey = configuration["Admin:Key"] ?? configuration["ADMIN_KEY"];
    }

    public bool Validate(string? suppliedKey)
        => !string.IsNullOrEmpty(_configuredKey)
           && !string.IsNullOrWhiteSpace(suppliedKey)
           && _configuredKey == suppliedKey;
}