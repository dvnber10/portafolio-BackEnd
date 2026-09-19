using Microsoft.EntityFrameworkCore;
using PortfolioApi.Domain.Entities;

namespace PortfolioApi.Application.Common;

public interface IPortfolioDbContext
{
    DbSet<GeneralCvData> GeneralCvData { get; }
    DbSet<SiteStats> SiteStats { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}