using Shared.Services;

namespace CourierService.Api.CompositionRoot;

public sealed record CourierServiceSettings(
    bool UseInMemory,
    string PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers)
{
    public static CourierServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                          || string.Equals(
                              configuration["UseInMemoryDb"],
                              "true",
                              StringComparison.OrdinalIgnoreCase);

        return new CourierServiceSettings(
            useInMemory,
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"));
    }
}
