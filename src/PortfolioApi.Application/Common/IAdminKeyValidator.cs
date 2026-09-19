namespace PortfolioApi.Application.Common;

public interface IAdminKeyValidator
{
    bool Validate(string? suppliedKey);
}