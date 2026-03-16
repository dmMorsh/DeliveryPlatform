using Shared.Services;

namespace DeliveryService.Api.CompositionRoot;

public sealed record DeliveryServiceSettings(
    bool UseInMemory,
    string? PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers,
    int HttpTimeoutSeconds)
{
    public static DeliveryServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        var postgresConnectionString = useInMemory
            ? null
            : ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL");

        var httpTimeoutSeconds = int.TryParse(configuration["Http:TimeoutSeconds"], out var httpTimeout)
            ? httpTimeout
            : 10;

        return new DeliveryServiceSettings(
            useInMemory,
            postgresConnectionString,
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            httpTimeoutSeconds);
    }
}
