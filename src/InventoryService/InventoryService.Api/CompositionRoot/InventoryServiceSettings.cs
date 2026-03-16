using Shared.Services;

namespace InventoryService.Api.CompositionRoot;

public sealed record InventoryServiceSettings(
    bool UseInMemory,
    string? PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers,
    int ShardCount)
{
    public static InventoryServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        var postgresConnectionString = useInMemory
            ? null
            : ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL");

        return new InventoryServiceSettings(
            useInMemory,
            postgresConnectionString,
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            configuration.GetValue("ShardCount", 1));
    }
}
