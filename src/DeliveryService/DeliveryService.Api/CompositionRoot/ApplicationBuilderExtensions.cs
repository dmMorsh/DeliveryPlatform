using Hangfire;
using DeliveryService.Infrastructure.Jobs;
using DeliveryService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DeliveryService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapDeliveryServiceHealthChecks(this IEndpointRouteBuilder endpoints)
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

    public static void RunDeliveryServiceMigrationsAndJobs(
        this WebApplication app,
        bool useInMemory,
        IConfiguration configuration)
    {
        if (useInMemory)
        {
            Log.Information("Using in-memory database; skipping migrations.");
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        dbContext.Database.Migrate();
        Log.Information("Database migration completed for DeliveryService");

        var enabled = bool.TryParse(configuration["Delivery:SlaEnabled"], out var slaEnabled)
            ? slaEnabled
            : true;
        if (enabled)
        {
            var cron = configuration["Delivery:SlaCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 8,20 * * *";

            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate<IDeliverySlaJob>(
                "delivery-sla",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }
    }
}
