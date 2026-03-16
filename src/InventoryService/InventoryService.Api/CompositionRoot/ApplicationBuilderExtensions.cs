using Hangfire;
using InventoryService.Infrastructure.Jobs;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.ReadStore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InventoryService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapInventoryServiceHealthChecks(this IEndpointRouteBuilder endpoints)
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

    public static void RunInventoryServiceMigrationsAndJobs(
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
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        dbContext.Database.Migrate();
        var inventoryReadDbContext = scope.ServiceProvider.GetRequiredService<InventoryReadDbContext>();
        inventoryReadDbContext.Database.Migrate();
        Log.Information("Database migration completed for InventoryService");

        var enabled = bool.TryParse(configuration["Inventory:ReservationAlertEnabled"], out var alertEnabled)
            ? alertEnabled
            : true;
        if (enabled)
        {
            var cron = configuration["Inventory:ReservationAlertCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 4 * * *";

            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate<IInventoryReservationAlertJob>(
                "inventory-reservation-alert",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }
    }
}
