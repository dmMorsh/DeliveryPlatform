using Shared.Services;

namespace CatalogReadService.Api.CompositionRoot;

public sealed record CatalogReadServiceSettings(
    string RedisConnectionString,
    string KafkaBrokers,
    string ElasticsearchUrl)
{
    public static CatalogReadServiceSettings From(IConfiguration configuration, IHostEnvironment environment)
    {
        return new CatalogReadServiceSettings(
            ConfigurationGuard.GetRequired(configuration, environment, "Redis:Connection", "localhost:6379"),
            ConfigurationGuard.GetRequired(configuration, environment, "Kafka:Brokers", "localhost:29092"),
            ConfigurationGuard.GetRequired(configuration, environment, "Elasticsearch:Url", "http://localhost:9200"));
    }
}
