using Shared.Services;

namespace OrderReadService.Api.CompositionRoot;

public sealed record OrderReadServiceSettings(
    string PostgresConnectionString,
    string RedisConnectionString,
    string KafkaBrokers)
{
    public static OrderReadServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        return new OrderReadServiceSettings(
            ConfigurationGuard.GetRequiredConnectionString(configuration, environment, "PostgreSQL"),
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"));
    }
}
