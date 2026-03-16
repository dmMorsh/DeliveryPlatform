using Shared.Services;

namespace OrderService.Api.CompositionRoot;

public sealed record OrderServiceSettings(
    bool UseInMemory,
    string? PostgresConnectionString,
    string? HangfireConnectionString,
    string KafkaBrokers,
    string? RedisConnectionString,
    int ShardCount)
{
    public static OrderServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        var postgresConnectionString = useInMemory
            ? null
            : ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL");

        return new OrderServiceSettings(
            useInMemory,
            postgresConnectionString,
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "Hangfire"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            configuration.GetValue("ShardCount", 1));
    }
}
