using Shared.Services;

namespace PaymentService.Api.CompositionRoot;

public sealed record PaymentServiceSettings(
    string PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers,
    string? HangfireConnectionString,
    int HttpTimeoutSeconds)
{
    public static PaymentServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var httpTimeoutSeconds = int.TryParse(configuration["Http:TimeoutSeconds"], out var httpTimeout)
            ? httpTimeout
            : 10;

        return new PaymentServiceSettings(
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "Hangfire"),
            httpTimeoutSeconds);
    }
}
