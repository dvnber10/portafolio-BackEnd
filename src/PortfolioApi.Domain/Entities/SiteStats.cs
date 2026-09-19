using PortfolioApi.Domain.Common;

namespace PortfolioApi.Domain.Entities;

public class SiteStats : BaseEntity
{
    public const string SingletonId = "b7c4d21e-0000-4000-9000-000000000001";
    public long VisitCount { get; set; }
}