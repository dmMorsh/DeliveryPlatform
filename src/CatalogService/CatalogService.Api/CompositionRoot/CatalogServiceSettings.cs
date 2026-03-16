using Shared.Services;

namespace CatalogService.Api.CompositionRoot;

public sealed record CatalogServiceSettings(
    bool UseInMemory,
    string PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers)
{
    public static CatalogServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        return new CatalogServiceSettings(
            useInMemory,
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"));
    }
}
