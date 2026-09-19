using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioApi.Application.Services;
using PortfolioApi.Infrastructure.Persistence;

namespace PortfolioApi.Api.Controllers;

/// <summary>Panel de estado y documentación de la API (público, sin autenticación).</summary>
[ApiController]
[Route("")]
public class DocsController : ControllerBase
{
    private static readonly DateTime StartedAt = DateTime.UtcNow;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly PortfolioDbContext _db;
    private readonly IPortfolioService _service;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DocsController> _logger;

    public DocsController(
        PortfolioDbContext db,
        IPortfolioService service,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DocsController> logger)
    {
        _db = db;
        _service = service;
        _config = config;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => Content(await BuildHtmlAsync(ct), "text/html; charset=utf-8");

    [HttpGet("api/health")]
    public async Task<ActionResult<object>> Health(CancellationToken ct)
        => Ok(await BuildStatusAsync(ct));

    private async Task<object> BuildStatusAsync(CancellationToken ct)
    {
        var dbStatus = await CheckDatabaseAsync(ct);

        object dataSummary = new { profiles = 0, projects = 0, experience = 0, education = 0, languages = 0, slugs = Array.Empty<string>() };
        if (dbStatus.Connected)
        {
            try
            {
                var doc = await _service.GetCvDocumentAsync(ct);
                dataSummary = Summarize(doc.Json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("No se pudo leer el documento CV para el panel: {Error}", ex.Message);
            }
        }

        return new
        {
            service = "PortfolioApi",
            version = AssemblyVersion,
            status = "ok",
            environment = _env.EnvironmentName,
            serverTimeUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            uptime = (DateTime.UtcNow - StartedAt).ToString(@"d\.hh\:mm\:ss"),
            processId = Environment.ProcessId,
            database = dbStatus,
            config = new
            {
                adminKeyConfigured = ConfigHas("Admin:Key") || ConfigHas("ADMIN_KEY"),
                databaseConnectionConfigured = ConfigHas("Database:Connection"),
                corsOrigins = _config["Cors:Origins"] ?? "",
                swaggerEnabled = true
            },
            data = dataSummary,
            endpoints = EndpointDocs.Select(e => new { e.Method, e.Path, e.Description, e.Auth })
        };
    }

    private async Task<string> BuildHtmlAsync(CancellationToken ct)
    {
        var status = JsonSerializer.SerializeToElement(await BuildStatusAsync(ct), WebJson);
        var dbEl = status.GetProperty("database");
        var configEl = status.GetProperty("config");
        var dataEl = status.GetProperty("data");
        var dbOk = dbEl.GetProperty("connected").GetBoolean();
        var adminOk = configEl.GetProperty("adminKeyConfigured").GetBoolean();
        var connOk = configEl.GetProperty("databaseConnectionConfigured").GetBoolean();

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>PortfolioApi · Estado</title><style>");
        sb.Append("body{font-family:-apple-system,'Segoe UI',Roboto,Arial,sans-serif;background:#f4f6f9;color:#1f2937;margin:0;padding:24px}");
        sb.Append("*{box-sizing:border-box}h1{font-size:22px;margin:0 0 4px}.wrap{max-width:960px;margin:0 auto}");
        sb.Append(".badge{display:inline-block;padding:2px 10px;border-radius:12px;font-size:12px;font-weight:600;color:#fff}");
        sb.Append(".ok{background:#16a34a}.bad{background:#dc2626}");
        sb.Append(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:14px;margin:18px 0}");
        sb.Append(".card{background:#fff;border:1px solid #e5e7eb;border-radius:10px;padding:16px}");
        sb.Append(".card h2{font-size:13px;margin:0 0 10px;text-transform:uppercase;color:#374151;letter-spacing:.03em}");
        sb.Append(".row{display:flex;justify-content:space-between;padding:3px 0;font-size:13px;word-break:break-word}");
        sb.Append(".row b{font-weight:600}.muted{color:#6b7280}");
        sb.Append("table{width:100%;border-collapse:collapse;background:#fff;border:1px solid #e5e7eb;border-radius:10px;overflow:hidden;font-size:13px;margin-top:8px}");
        sb.Append("th,td{text-align:left;padding:8px 10px;border-bottom:1px solid #f0f1f3}th{background:#fafbfc;text-transform:uppercase;font-size:11px;color:#6b7280}");
        sb.Append("code{background:#eef1f5;padding:1px 6px;border-radius:5px;font-size:12px}a{color:#2E6DA4}.green{color:#16a34a;font-weight:700}.red{color:#dc2626;font-weight:700}");
        sb.Append(".mono{margin:6px 0}section{margin-top:18px}h3{font-size:16px;margin:0 0 6px}");
        sb.Append("</style></head><body><div class=\"wrap\">");

        sb.Append("<h1>PortfolioApi <span class=\"badge ").Append(dbOk ? "ok" : "bad").Append("\">")
          .Append(dbOk ? "EN L\u00cdNEA" : "ATENCI\u00d3N").Append("</span></h1>");
        sb.Append("<p class=\"muted\">Estado y documentaci\u00f3n de la API. Tambi\u00e9n disponible en JSON: <code>/api/health</code>.</p>");

        sb.Append("<div class=\"grid\">");

        sb.Append("<div class=\"card\"><h2>Servicio</h2>");
        Row(sb, "Version", status.GetProperty("version").GetString());
        Row(sb, "Ambiente", status.GetProperty("environment").GetString());
        Row(sb, "Hora (UTC)", status.GetProperty("serverTimeUtc").GetString());
        Row(sb, "Uptime", status.GetProperty("uptime").GetString());
        Row(sb, "PID", status.GetProperty("processId").GetInt32().ToString());
        sb.Append("</div>");

        sb.Append("<div class=\"card\"><h2>Base de datos</h2>");
        Row(sb, "Conexi\u00f3n", dbOk ? "OK" : "FALLO", dbOk ? "green" : "red");
        Row(sb, "Migraciones", dbEl.GetProperty("migrationsApplied").GetBoolean() ? "Aplicadas" : "Pendientes", "muted");
        Row(sb, "VisitCount", dbEl.GetProperty("visitCount").GetInt64().ToString());
        Row(sb, "Filas CvData", dbEl.GetProperty("cvDataRows").GetInt32().ToString());
        if (!dbOk) Row(sb, "Detalle", dbEl.GetProperty("error").GetString(), "muted");
        sb.Append("</div>");

        sb.Append("<div class=\"card\"><h2>Configuraci\u00f3n</h2>");
        Row(sb, "Admin key", adminOk ? "Configurada" : "FALTA", adminOk ? "green" : "red");
        Row(sb, "Cadena SQL", connOk ? "Configurada" : "FALTA", connOk ? "green" : "red");
        Row(sb, "CORS", configEl.GetProperty("corsOrigins").GetString());
        sb.Append("</div>");

        sb.Append("<div class=\"card\"><h2>Contenido (json)</h2>");
        Row(sb, "Perfiles", dataEl.GetProperty("profiles").GetInt32().ToString());
        Row(sb, "Proyectos", dataEl.GetProperty("projects").GetInt32().ToString());
        Row(sb, "Experiencia", dataEl.GetProperty("experience").GetInt32().ToString());
        Row(sb, "Educaci\u00f3n", dataEl.GetProperty("education").GetInt32().ToString());
        Row(sb, "Idiomas", dataEl.GetProperty("languages").GetInt32().ToString());
        Row(sb, "Slugs", string.Join(", ", dataEl.GetProperty("slugs").EnumerateArray().Select(s => s.GetString())));
        sb.Append("</div>");
        sb.Append("</div>");

        sb.Append("<section><h3>Endpoints</h3><table><thead><tr><th>M\u00e9todo</th><th>Ruta</th><th>Descripci\u00f3n</th><th>Auth</th></tr></thead><tbody>");
        foreach (var e in EndpointDocs)
            sb.Append("<tr><td><code>").Append(e.Method).Append("</code></td><td><code>").Append(e.Path).Append("</code></td><td>").Append(e.Description).Append("</td><td>").Append(e.Auth).Append("</td></tr>");
        sb.Append("</tbody></table></section>");

        sb.Append("<section><h3>Comprobaciones r\u00e1pidas</h3>");
        sb.Append("<div class=\"mono\"><code>GET /api/health</code> <span class=\"muted\">este panel en JSON</span></div>");
        sb.Append("<div class=\"mono\"><code><a href=\"/api/portfolio\">GET /api/portfolio</a></code> <span class=\"muted\">resumen del portafolio</span></div>");
        sb.Append("<div class=\"mono\"><code><a href=\"/api/cv\">GET /api/cv</a></code> <span class=\"muted\">documento CV completo</span></div>");
        sb.Append("<div class=\"mono\"><code><a href=\"/api/cvs/IA?download=1\">GET /api/cvs/IA?download=1</a></code> <span class=\"muted\">descarga un PDF por perfil</span></div>");
        sb.Append("<div class=\"mono\"><code><a href=\"/swagger\">GET /swagger</a></code> <span class=\"muted\">documentaci\u00f3n OpenAPI interactiva</span></div>");
        sb.Append("<div class=\"mono\"><code>POST /api/stats/visits</code> <span class=\"muted\">registra una visita</span></div>");
        sb.Append("</section>");

        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    private async Task<DbStatus> CheckDatabaseAsync(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var connected = await _db.Database.CanConnectAsync(timeout.Token);
            if (!connected)
                return new DbStatus(false, 0, 0, false, "No se pudo conectar a la base de datos (revisa Database:Connection) y el firewall.");

            var cvDataRows = await _db.GeneralCvData.CountAsync(timeout.Token);
            var visitCount = await _db.SiteStats.SumAsync(s => (long?)s.VisitCount, timeout.Token) ?? 0;
            var migrationsApplied = true;
            try
            {
                var pending = await _db.Database.GetPendingMigrationsAsync(timeout.Token);
                migrationsApplied = !pending.Any();
            }
            catch { migrationsApplied = false; }

            return new DbStatus(true, cvDataRows, visitCount, migrationsApplied, "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Fallo de conexi\u00f3n a la BD: {Error}", ex.Message);
            return new DbStatus(false, 0, 0, false, ex.Message);
        }
    }

    private static object Summarize(string json)
    {
        var (profiles, projects, experience, education, languages, slugs) = SummarizeJson(json);
        return new { profiles, projects, experience, education, languages, slugs };
    }

    private static (int Profiles, int Projects, int Experience, int Education, int Languages, string[] Slugs) SummarizeJson(string json)
    {
        var empty = (0, 0, 0, 0, 0, Array.Empty<string>());
        if (string.IsNullOrWhiteSpace(json)) return empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            static int Count(JsonElement el, string name)
                => el.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array ? arr.GetArrayLength() : 0;
            var slugs = root.TryGetProperty("profiles", out var profiles) && profiles.ValueKind == JsonValueKind.Array
                ? profiles.EnumerateArray()
                    .Select(p => p.TryGetProperty("slug", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!).ToArray()
                : Array.Empty<string>();
            return (Count(root, "profiles"), Count(root, "projects"), Count(root, "experience"), Count(root, "education"), Count(root, "languages"), slugs);
        }
        catch
        {
            return empty;
        }
    }

    private bool ConfigHas(string key) => !string.IsNullOrWhiteSpace(_config[key]);

    private static string AssemblyVersion { get; } =
        typeof(DocsController).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    private static void Row(StringBuilder sb, string label, string? value, string cls = "")
    {
        sb.Append("<div class=\"row\"><b>").Append(label).Append("</b><span class=\"").Append(cls).Append("\">")
          .Append(WebUtility.HtmlEncode(value ?? "—")).Append("</span></div>");
    }

    private static readonly (string Method, string Path, string Description, string Auth)[] EndpointDocs =
    {
        ("GET", "/", "Panel de estado y documentaci\u00f3n (esta p\u00e1gina)", "P\u00fablico"),
        ("GET", "/swagger", "Documentaci\u00f3n OpenAPI interactiva", "P\u00fablico"),
        ("GET", "/api/health", "Estado del servicio en JSON (BD, migraciones, datos, config)", "P\u00fablico"),
        ("GET", "/api/portfolio", "Resumen del portafolio (personal + perfiles)", "P\u00fablico"),
        ("GET", "/api/cv", "Documento CV completo (fuente \u00fanica)", "P\u00fablico"),
        ("GET", "/api/portfolio/{slug}", "P\u00e1gina de un perfil (proyectos, skills, etc.)", "P\u00fablico"),
        ("GET", "/api/cvs/{slug}", "PDF del CV de un perfil (?download=1 fuerza descarga; si no, previsualiza)", "P\u00fablico"),
        ("GET", "/api/stats/visits", "N\u00famero de visitas acumuladas", "P\u00fablico"),
        ("POST", "/api/stats/visits", "Registra una visita", "P\u00fablico"),
        ("POST", "/api/admin/auth", "Valida la clave del panel (header X-Admin-Key)", "No requiere (valida clave)"),
        ("GET", "/api/admin/cv", "Documento CV (edici\u00f3n)", "Admin"),
        ("PUT", "/api/admin/cv", "Actualiza el CV desde el panel", "Admin"),
        ("GET", "/api/admin/portfolio", "Resumen del portafolio (edici\u00f3n)", "Admin"),
    };
}

internal sealed record DbStatus(bool Connected, int CvDataRows, long VisitCount, bool MigrationsApplied, string Error);