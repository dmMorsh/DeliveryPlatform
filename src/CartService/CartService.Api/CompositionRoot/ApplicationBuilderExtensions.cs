using CartService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CartService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapCartServiceHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = reg => reg.Tags.Contains("ready")
        });
        return endpoints;
    }

    public static void RunCartServiceMigrations(this WebApplication app, bool useInMemory)
    {
        if (useInMemory)
        {
            Log.Information("Using in-memory database; skipping migrations.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        dbContext.Database.Migrate();
        Log.Information("Database migration completed for CartService");
    }
}
