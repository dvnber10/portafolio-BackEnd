using Microsoft.EntityFrameworkCore;
using PortfolioApi.Application.Common;
using PortfolioApi.Domain.Entities;

namespace PortfolioApi.Infrastructure.Persistence;

public class PortfolioDbContext : DbContext, IPortfolioDbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<GeneralCvData> GeneralCvData => Set<GeneralCvData>();
    public DbSet<SiteStats> SiteStats => Set<SiteStats>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneralCvData>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.DataJson).HasColumnType("nvarchar(max)").IsRequired();
        });

        modelBuilder.Entity<SiteStats>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.VisitCount).IsRequired();
        });
    }
}