using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Application.Interfaces;
using OrderService.Application.MediatR;
using OrderService.Application.Services;
using OrderService.Application.Utils;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.Jobs;
using OrderService.Infrastructure.Mapping;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace OrderService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServiceApi(this IServiceCollection services)
    {
        services.AddGrpc();
        services.AddControllers();
        services.AddOpenApi();
        return services;
    }

    public static IServiceCollection AddOrderServiceCore(
        this IServiceCollection services,
        IConfiguration configuration,
        OrderServiceSettings settings)
    {
        AddData(services, settings);
        AddMessaging(services, configuration);
        AddCaching(services, settings);
        AddOutboxAndJobs(services, settings.UseInMemory);
        AddMediatR(services);
        return services;
    }

    public static IServiceCollection AddOrderServiceHealthChecks(
        this IServiceCollection services,
        OrderServiceSettings settings)
    {
        var healthChecks = services.AddHealthChecks()
            .AddKafka(
                new ProducerConfig
                {
                    BootstrapServers = settings.KafkaBrokers
                },
                name: "kafka",
                tags: new[] { "ready" });

        if (!settings.UseInMemory && !string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
        {
            healthChecks.AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" });
            // Outbox lag health check
            healthChecks.AddCheck<OutboxLagHealthCheck>("outbox_lag", tags: new[] { "ready" });
        }

        return services;
    }

    public static IServiceCollection AddAdaptiveThrottle(
        this IServiceCollection services,
        OrderServiceSettings settings)
    {
        // Adaptive throttle service monitors health and adjusts rate limits
        if (settings.UseInMemory || string.IsNullOrWhiteSpace(settings.RedisConnectionString))
            return services;

        services.AddSingleton<AdaptiveThrottleService>(sp =>
            new AdaptiveThrottleService(
                sp.GetRequiredService<IDistributedRateLimiter>(),
                sp.GetRequiredService<HealthCheckService>(),
                sp.GetRequiredService<ILogger<AdaptiveThrottleService>>(),
                async () => (await sp.GetRequiredService<OrderDbContext>().OutboxMessages.CountAsync())));
        services.AddHostedService(sp => sp.GetRequiredService<AdaptiveThrottleService>());

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        OrderServiceSettings settings)
    {
        if (settings.UseInMemory)
        {
            services.AddDbContext<OrderDbContext>(options =>
                options.UseInMemoryDatabase("orders_inmem"));
            services.AddDbContext<KitchenDbContext>(options =>
                options.UseInMemoryDatabase("kitchen_inmem"));
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");

        services.AddDbContextPool<OrderDbContext>(options =>
            options.UseNpgsql(settings.PostgresConnectionString));
        services.AddDbContextPool<KitchenDbContext>(options =>
            options.UseNpgsql(settings.PostgresConnectionString));

        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

        if (!string.IsNullOrWhiteSpace(settings.HangfireConnectionString))
            config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(settings.HangfireConnectionString));
        else
            config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(settings.PostgresConnectionString));
    });
    services.AddHangfireServer();

    // Sharding
    services.AddSingleton<IShardResolver>(sp =>
    {
        return new HashShardResolver(settings.ShardCount);
    });
    services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
    }

    private static void AddMessaging(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        // Ensure Kafka topics exist on startup
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddSingleton<IOrderIntegrationEventMapper, IntegrationEventMapper>();
        // Event Consumer from other services
        services.AddSingleton<OrderEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<OrderEventConsumer>>();
        services.AddScoped<IEventInbox, DbEventInbox<OrderDbContext>>();

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.Configure<KitchenCapacityOptions>(
            configuration.GetSection("Kitchen"));
        services.Configure<DeliveryZoneOptions>(
            configuration.GetSection("DeliveryZones"));
        services.AddSingleton<IDeliveryZoneMatcher, DeliveryZoneMatcher>();
        services.AddScoped<IKitchenSlotRepository, KitchenSlotRepository>();
    }

    private static void AddCaching(
        IServiceCollection services,
        OrderServiceSettings settings)
    {
        // Register kitchen slot cache: Noop by default, Redis if configured
        services.AddSingleton<IKitchenSlotCache, NoopKitchenSlotCache>();
        if (!string.IsNullOrWhiteSpace(settings.RedisConnectionString))
        {
            var redisConnection = ConnectionMultiplexer.Connect(settings.RedisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(_ => redisConnection);
            services.AddSingleton<IKitchenSlotCache, RedisKitchenSlotCache>();
            services.AddSingleton<IDistributedRateLimiter>(sp =>
                new DistributedRateLimiter(
                    redisConnection,
                sp.GetRequiredService<ILogger<DistributedRateLimiter>>(),
                "order-service"));
        }
        else
        {
            services.AddSingleton<IDistributedRateLimiter>(sp =>
                new DistributedRateLimiter(
                    ConnectionMultiplexer.Connect("localhost"),
                sp.GetRequiredService<ILogger<DistributedRateLimiter>>(),
                "order-service"));
        }
    }

    private static void AddOutboxAndJobs(IServiceCollection services, bool useInMemory)
    {
        if (useInMemory)
            return;

        // Only run OutboxProcessor when using a real relational DB
        services.AddHostedService(sp =>
            new OutboxProcessor<OrderDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<OrderDbContext>>>(),
                schema: "order"));
        services.AddHostedService<OutboxCleanupHostedService<OrderDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<OrderDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<OrderDbContext, ProcessedCommand>>();
        services.AddSingleton<IOrderPaymentTtlJob, OrderPaymentTtlJob>();
        services.AddSingleton<IOrderAssigningTtlJob, OrderAssigningTtlJob>();
        services.AddSingleton<IOrderKitchenAcceptanceTtlJob, OrderKitchenAcceptanceTtlJob>();
        services.AddSingleton<IOrderKitchenDelayJob, OrderKitchenDelayJob>();
    }

    private static void AddMediatR(IServiceCollection services)
    {
        // Register MediatR handlers from Application assembly
        services
            .AddMediatR(typeof(ApplicationMarker).Assembly)
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        services.AddScoped<IHangfireCommandExecutor>(sp =>
            new HangfireCommandExecutor<OrderDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<OrderDbContext>(),
                "order"));
    }
}
