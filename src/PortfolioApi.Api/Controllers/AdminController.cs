using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Api.Auth;
using PortfolioApi.Application.Common;
using PortfolioApi.Application.Dtos;
using PortfolioApi.Application.Services;

namespace PortfolioApi.Api.Controllers;

[ApiController]
[Route("api/admin")]
[AdminAuthorize]
public class AdminController : ControllerBase
{
    private readonly IPortfolioService _service;

    public AdminController(IPortfolioService service) => _service = service;

    [AllowAnonymous]
    [HttpPost("auth")]
    public IActionResult Authenticate([FromServices] IAdminKeyValidator validator)
    {
        var key = Request.Headers[AdminAuthorizeAttribute.HeaderName].FirstOrDefault();
        return Ok(new { valid = validator.Validate(key) });
    }

    [HttpGet("cv")]
    public async Task<ActionResult<CvDocumentDto>> GetCv(CancellationToken ct)
        => Ok(await _service.GetCvDocumentAsync(ct));

    [HttpPut("cv")]
    public async Task<ActionResult<SaveCvResultDto>> SaveCv([FromBody] SaveCvRequest request, CancellationToken ct)
    {
        var result = await _service.SaveCvDocumentAsync(request.Json ?? string.Empty, ct);
        return result.Ok ? Ok(result) : BadRequest(result);
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolio(CancellationToken ct)
        => Ok(await _service.GetPortfolioSummaryAsync(ct));
}

public record SaveCvRequest(string? Json);