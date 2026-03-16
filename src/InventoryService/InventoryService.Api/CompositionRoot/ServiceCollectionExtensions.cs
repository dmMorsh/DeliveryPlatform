using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using InventoryService.Application.Interfaces;
using InventoryService.Application.MediatR;
using InventoryService.Application.Read;
using InventoryService.Application.Services;
using InventoryService.Application.Utils;
using InventoryService.Infrastructure.Jobs;
using InventoryService.Infrastructure.Mapping;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.ReadStore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace InventoryService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        return services;
    }

    public static IServiceCollection AddInventoryServiceCore(
        this IServiceCollection services,
        InventoryServiceSettings settings)
    {
        AddData(services, settings);
        AddMediatR(services, settings.UseInMemory);
        AddMessaging(services);
        AddCaching(services, settings.RedisConnectionString);
        AddReadStore(services, settings.PostgresConnectionString);

        return services;
    }

    public static IServiceCollection AddInventoryServiceHealthChecks(
        this IServiceCollection services,
        InventoryServiceSettings settings)
    {
        var healthChecks = services.AddHealthChecks()
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka");

        if (!settings.UseInMemory)
        {
            if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
                throw new InvalidOperationException("PostgreSQL connection string is required.");
            healthChecks.AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" });
        }

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        InventoryServiceSettings settings)
    {
        if (settings.UseInMemory)
        {
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseInMemoryDatabase("orders_inmem"));
            services.AddScoped<IUnitOfWorkFactory, MemUnitOfWorkFactory>();
            return;
        }

        // DbContext: for OutboxProcessor and Hangfire
        if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");

        var connectionString = settings.PostgresConnectionString;
        services.AddDbContextPool<InventoryDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Outbox processor
        services.AddHostedService(sp =>
            new OutboxProcessor<InventoryDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<InventoryDbContext>>>(),
                schema: "inventory"));
        services.AddHostedService<OutboxCleanupHostedService<InventoryDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<InventoryDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<InventoryDbContext, ProcessedCommand>>();

        // Hangfire
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true }));
        services.AddHangfireServer();
        services.AddScoped<IHangfireCommandExecutor>(sp =>
            new HangfireCommandExecutor<InventoryDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<InventoryDbContext>(),
                "inventory"));
        services.AddSingleton<IInventoryReservationAlertJob, InventoryReservationAlertJob>();

        // Sharding
        services.AddSingleton<IShardResolver>(new HashShardResolver(settings.ShardCount));
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
    }

    private static void AddMediatR(IServiceCollection services, bool useInMemory)
    {
        services
            .AddMediatR(typeof(ApplicationMarker).Assembly)
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        if (!useInMemory)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
    }

    private static void AddMessaging(IServiceCollection services)
    {
        // Kafka Event Producer
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        // Ensure Kafka topics exist on startup
        services.AddHostedService<KafkaTopicBootstrapper>();
        // Event Consumer from OrderService
        services.AddSingleton<InventoryEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<InventoryEventConsumer>>();
        services.AddScoped<IEventInbox, DbEventInbox<InventoryDbContext>>();

        services.AddSingleton<IStockIntegrationEventMapper, StockIntegrationEventMapper>();

    }

    private static void AddReadStore(
        IServiceCollection services,
        string? postgresConnectionString)
    {
        // read-store context for inventory
        services.AddScoped<IInventoryReadCache, InventoryReadRedisCache>();
        if (string.IsNullOrWhiteSpace(postgresConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");
        services.AddDbContextPool<InventoryReadDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));
        services.AddScoped<IInventoryReadRepository, InventoryReadRepository>();
        services.AddScoped<InventoryReadProjector>();

        services.AddSingleton<InventoryReadProjectionConsumer>(sp =>
            new InventoryReadProjectionConsumer(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IHostEnvironment>(),
                sp.GetRequiredService<ILogger<InventoryReadProjectionConsumer>>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>()));
        services.AddHostedService<KafkaEventConsumerHostedService<InventoryReadProjectionConsumer>>();
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
}
