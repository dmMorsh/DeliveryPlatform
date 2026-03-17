using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Providers;
using Serilog;

namespace PaymentService.Api.CompositionRoot;

public static class ApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder MapPaymentServiceHealthChecks(this IEndpointRouteBuilder endpoints)
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

    public static void ValidatePaymentProviders(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
            return;

        var sberbank = app.Configuration.GetSection("Payments:Sberbank").Get<SberbankOptions>();
        var yoomoney = app.Configuration.GetSection("Payments:YooMoney").Get<YooMoneyOptions>();

        var hasSberbank = sberbank is not null
                          && !string.IsNullOrWhiteSpace(sberbank.BaseUrl)
                          && !string.IsNullOrWhiteSpace(sberbank.UserName)
                          && !string.IsNullOrWhiteSpace(sberbank.Password)
                          && !string.IsNullOrWhiteSpace(sberbank.ReturnUrl)
                          && !string.IsNullOrWhiteSpace(sberbank.FailUrl);

        var hasYooMoney = yoomoney is not null
                          && !string.IsNullOrWhiteSpace(yoomoney.BaseUrl)
                          && !string.IsNullOrWhiteSpace(yoomoney.SecretKey)
                          && !string.IsNullOrWhiteSpace(yoomoney.ShopId)
                          && !string.IsNullOrWhiteSpace(yoomoney.ReturnUrl)
                          && !string.IsNullOrWhiteSpace(yoomoney.FailUrl);

        if (!hasSberbank && !hasYooMoney)
            throw new InvalidOperationException("At least one payment provider must be configured in production.");
    }

    public static void RunPaymentServiceMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        dbContext.Database.Migrate();
        var shardContext = scope.ServiceProvider.GetRequiredService<PaymentShardMapDbContext>();
        shardContext.Database.Migrate();
        Log.Information("Database migration completed for PaymentService");
    }
}
