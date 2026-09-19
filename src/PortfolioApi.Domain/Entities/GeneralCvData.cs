using PortfolioApi.Domain.Common;

namespace PortfolioApi.Domain.Entities;

public class GeneralCvData : BaseEntity
{
    public const string SingletonId = "a3f7c12e-0000-4000-8000-000000000001";
    public string DataJson { get; set; } = "{}";
}