using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CatalogService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapCatalogServiceHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });
        endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = reg => reg.Tags.Contains("ready")
        });
        return endpoints;
    }

    public static void RunCatalogServiceMigrations(this WebApplication app, bool useInMemory)
    {
        if (useInMemory)
        {
            Log.Information("Using in-memory database; skipping migrations.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        dbContext.Database.Migrate();
        Log.Information("Database migration completed for CatalogService");
    }
}
