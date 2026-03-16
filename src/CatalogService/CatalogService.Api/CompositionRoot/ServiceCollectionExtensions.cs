using CatalogService.Application.Interfaces;
using CatalogService.Application.MediatR;
using CatalogService.Application.Services;
using CatalogService.Infrastructure.Mapping;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Repositories;
using CatalogService.Infrastructure.Services;
using Confluent.Kafka;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace CatalogService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        return services;
    }

    public static IServiceCollection AddCatalogServiceCore(
        this IServiceCollection services,
        IHostEnvironment environment,
        CatalogServiceSettings settings)
    {
        AddData(services, settings.PostgresConnectionString, settings.UseInMemory);
        AddMediatR(services);
        AddMessaging(services);
        AddCaching(services, settings.RedisConnectionString);
        AddOutbox(services, settings.UseInMemory);
        AddHangfire(services, settings.PostgresConnectionString, environment);

        return services;
    }

    public static IServiceCollection AddCatalogServiceHealthChecks(
        this IServiceCollection services,
        CatalogServiceSettings settings)
    {
        var healthChecks = services.AddHealthChecks()
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka");

        if (!settings.UseInMemory)
            healthChecks.AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" });

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        string connectionString,
        bool useInMemory)
    {
        if (useInMemory)
        {
            services.AddDbContext<CatalogDbContext>(options =>
                options.UseInMemoryDatabase("orders_inmem"));
            return;
        }

        services.AddDbContextPool<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddMediatR(IServiceCollection services)
    {
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        // Ensure Kafka topics exist on startup
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddScoped<IEventInbox, DbEventInbox<CatalogDbContext>>();
        // Event Consumer from OrderService and InventoryService
        services.AddSingleton<CatalogEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<CatalogEventConsumer>>();

        services.AddSingleton<IProductIntegrationEventMapper, ProductIntegrationEventMapper>();
        services.AddSingleton<ICatalogMetricsStore, RedisCatalogMetricsStore>();
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
            services.AddHealthChecks()
                .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
        }
        catch (Exception ex)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
        }
    }

    private static void AddOutbox(IServiceCollection services, bool useInMemory)
    {
        if (useInMemory)
            return;

        // Outbox processor
        services.AddHostedService(sp =>
            new OutboxProcessor<CatalogDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<CatalogDbContext>>>(),
                schema: "catalog"));
        services.AddHostedService<OutboxCleanupHostedService<CatalogDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<CatalogDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<CatalogDbContext, ProcessedCommand>>();
    }

    private static void AddHangfire(
        IServiceCollection services,
        string connectionString,
        IHostEnvironment environment)
    {
        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (!string.IsNullOrWhiteSpace(connectionString))
                config.UsePostgreSqlStorage(connectionString);
            else
            {
                if (environment.IsProduction())
                    throw new InvalidOperationException("Hangfire connection string is required in production.");
                config.UseMemoryStorage();
            }
        });
        services.AddHangfireServer();
        services.AddScoped<IHangfireCommandExecutor>(sp =>
            new HangfireCommandExecutor<CatalogDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<CatalogDbContext>(),
                "catalog"));
    }
}
