using Hangfire;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Jobs;
using OrderService.Infrastructure.Persistence;
using Serilog;

namespace OrderService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapOrderServiceHealthChecks(this IEndpointRouteBuilder endpoints)
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

    public static void RunOrderServiceMigrationsAndJobs(
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
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.Migrate();
        var kitchenDbContext = scope.ServiceProvider.GetRequiredService<KitchenDbContext>();
        kitchenDbContext.Database.Migrate();
        Log.Information("Database migration completed for OrderService");

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        var paymentEnabled = bool.TryParse(configuration["Order:PaymentTtlEnabled"], out var ttlEnabled)
            ? ttlEnabled
            : true;
        if (paymentEnabled)
        {
            var cron = configuration["Order:PaymentTtlCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 3 * * *";

            recurringJobManager.AddOrUpdate<IOrderPaymentTtlJob>(
                "order-payment-ttl",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }

        var assigningEnabled = bool.TryParse(configuration["Order:AssigningTtlEnabled"], out var assignEnabled)
            ? assignEnabled
            : true;
        if (assigningEnabled)
        {
            var cron = configuration["Order:AssigningTtlCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 5 * * *";

            recurringJobManager.AddOrUpdate<IOrderAssigningTtlJob>(
                "order-assigning-ttl",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }

        var kitchenEnabled = bool.TryParse(configuration["Order:KitchenAcceptTtlEnabled"], out var kitchenAcceptEnabled)
            ? kitchenAcceptEnabled
            : true;
        if (kitchenEnabled)
        {
            var cron = configuration["Order:KitchenAcceptTtlCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 6 * * *";

            recurringJobManager.AddOrUpdate<IOrderKitchenAcceptanceTtlJob>(
                "order-kitchen-accept-ttl",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }

        var kitchenDelayEnabled = bool.TryParse(configuration["Order:KitchenDelayEnabled"], out var kitchenDelayValue)
            ? kitchenDelayValue
            : true;
        if (kitchenDelayEnabled)
        {
            var cron = configuration["Order:KitchenDelayCron"];
            if (string.IsNullOrWhiteSpace(cron))
                cron = "0 12 * * *";

            recurringJobManager.AddOrUpdate<IOrderKitchenDelayJob>(
                "order-kitchen-delay",
                job => job.ExecuteAsync(CancellationToken.None),
                cron);
        }
    }
}
