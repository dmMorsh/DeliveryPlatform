using Shared.Services;

namespace CartService.Api.CompositionRoot;

public sealed record CartServiceSettings(
    bool UseInMemory,
    string PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers,
    string OrderGrpcUrl,
    int HttpTimeoutSeconds)
{
    public static CartServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        var httpTimeoutSeconds = int.TryParse(configuration["Http:TimeoutSeconds"], out var httpTimeout)
            ? httpTimeout
            : 10;

        return new CartServiceSettings(
            useInMemory,
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            ConfigurationGuard.GetRequired(configuration, environment, "gRPC:OrderService:Url", "https://localhost:7204"),
            httpTimeoutSeconds);
    }
}
