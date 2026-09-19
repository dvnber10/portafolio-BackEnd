using Microsoft.EntityFrameworkCore;
using PortfolioApi.Application;
using PortfolioApi.Infrastructure;
using PortfolioApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var origins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000,http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddPolicy("PortfolioCors", policy =>
{
    var originList = origins.ToList();

    if (originList.Count == 0 || originList.Contains("*"))
    {
        policy.AllowAnyOrigin();
    }
    else
    {
        policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin, originList));
        policy.AllowCredentials();
    }
    policy.AllowAnyHeader().AllowAnyMethod();
}));

static bool IsOriginAllowed(string origin, List<string> allowed)
{
    var host = Uri.TryCreate(origin, UriKind.Absolute, out var uri) ? uri.Host : origin;

    if (allowed.Any(o => string.Equals(o.Trim(), origin, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(o.Trim(), "*", StringComparison.Ordinal)))
        return true;

    // Vercel usa subdominios efímeros (*.vercel.app) que cambian entre deploys/pull-requests.
    return host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("No fue posible aplicar las migraciones: {Error}", ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("PortfolioCors");
app.UseAuthorization();
app.MapControllers();

app.Run();