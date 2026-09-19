using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Application.Dtos;
using PortfolioApi.Application.Services;

namespace PortfolioApi.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IPortfolioService _service;

    public StatsController(IPortfolioService service) => _service = service;

    [HttpGet("visits")]
    public async Task<ActionResult<VisitStatsDto>> GetVisits(CancellationToken ct)
        => Ok(new VisitStatsDto(await _service.GetVisitsAsync(ct)));

    [HttpPost("visits")]
    public async Task<ActionResult<VisitStatsDto>> RegisterVisit(CancellationToken ct)
        => Ok(new VisitStatsDto(await _service.RegisterVisitAsync(ct)));
}