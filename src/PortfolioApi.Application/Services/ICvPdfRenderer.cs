using PortfolioApi.Application.Models;

namespace PortfolioApi.Application.Services;

public interface ICvPdfRenderer
{
    byte[] Render(CvData data, string profileSlug);
}