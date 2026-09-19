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
    if (origins.Length > 0)
        policy.WithOrigins(origins);
    else
        policy.AllowAnyOrigin();
    policy.AllowAnyHeader().AllowAnyMethod();
}));

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