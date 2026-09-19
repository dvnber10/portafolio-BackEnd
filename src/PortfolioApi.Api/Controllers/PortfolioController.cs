using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Application.Dtos;
using PortfolioApi.Application.Services;

namespace PortfolioApi.Api.Controllers;

[ApiController]
[Route("api")]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _service;

    public PortfolioController(IPortfolioService service) => _service = service;

    [HttpGet("portfolio")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetLanding(CancellationToken ct)
        => Ok(await _service.GetPortfolioSummaryAsync(ct));

    [HttpGet("cv")]
    public async Task<ActionResult<CvDocumentDto>> GetCvData(CancellationToken ct)
        => Ok(await _service.GetCvDocumentAsync(ct));

    [HttpGet("portfolio/{slug}")]
    public async Task<ActionResult<ProfilePageDto>> GetProfile(string slug, CancellationToken ct)
    {
        var page = await _service.GetProfilePageAsync(slug, ct);
        return page is null ? NotFound(new { error = "Perfil no encontrado." }) : Ok(page);
    }

    [HttpGet("cvs/{slug}")]
    public async Task<IActionResult> GetCvPdf(string slug, CancellationToken ct)
    {
        try
        {
            var (pdf, fileName) = await _service.RenderProfilePdfAsync(slug, ct);
            var download = Request.Query.ContainsKey("download") && Request.Query["download"] != "0";
            if (download)
                return File(pdf, "application/pdf", fileName);
            return File(pdf, "application/pdf");
        }
        catch (Application.Common.NotFoundException)
        {
            return NotFound(new { error = "No hay datos para generar el CV." });
        }
    }
}