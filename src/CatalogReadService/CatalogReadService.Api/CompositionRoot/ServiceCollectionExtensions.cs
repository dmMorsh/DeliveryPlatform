using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.MediatR;
using CatalogReadService.Infrastructure.Repositories;
using CatalogReadService.Infrastructure.Services;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Shared.Services;
using StackExchange.Redis;

namespace CatalogReadService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogReadServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        return services;
    }

    public static IServiceCollection AddCatalogReadServiceCore(
        this IServiceCollection services,
        CatalogReadServiceSettings settings)
    {
        services.AddScoped<IProductReadRepository, ProductReadRepository>();

        AddElasticsearch(services, settings.ElasticsearchUrl);
        AddReadStore(services);
        AddCaching(services, settings.RedisConnectionString);
        AddMessaging(services);

        return services;
    }

    public static IServiceCollection AddCatalogReadServiceHealthChecks(
        this IServiceCollection services,
        CatalogReadServiceSettings settings)
    {
        services.AddHealthChecks()
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka", tags: new[] { "ready" })
            .AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" });

        return services;
    }

    private static void AddElasticsearch(IServiceCollection services, string elasticsearchUrl)
    {
        // Elasticsearch client
        var esSettings = new ElasticsearchClientSettings(new Uri(elasticsearchUrl));
        services.AddSingleton(new ElasticsearchClient(esSettings));
    }

    private static void AddReadStore(IServiceCollection services)
    {
        // Read-store projector and consumer
        services.AddScoped<ProductReadProjector>();
        services.AddSingleton<ProductReadProjectionConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<ProductReadProjectionConsumer>>();
    }

    private static void AddCaching(
        IServiceCollection services,
        string redisConnection)
    {
        try
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.AbortOnConnectFail = false;
            var mux = ConnectionMultiplexer.Connect(redisOptions);
            services.AddSingleton<IConnectionMultiplexer>(mux);
        }
        catch (Exception ex)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
        }
    }

    private static void AddMessaging(IServiceCollection services)
    {
        // Kafka producer for retry/DLQ
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        services.AddHostedService<KafkaTopicBootstrapper>();
    }
}
