using Confluent.Kafka;
using CourierService.Application.Interfaces;
using CourierService.Application.MediatR;
using CourierService.Application.Services;
using CourierService.Infrastructure.Mapping;
using CourierService.Infrastructure.Persistence;
using CourierService.Infrastructure.Repositories;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace CourierService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCourierServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        return services;
    }

    public static IServiceCollection AddCourierServiceCore(
        this IServiceCollection services,
        IHostEnvironment environment,
        CourierServiceSettings settings)
    {
        AddData(services, settings.PostgresConnectionString, settings.UseInMemory);
        AddMessaging(services);
        AddOutbox(services, settings.UseInMemory);
        AddHangfire(services, settings.PostgresConnectionString, environment);
        AddDomainServices(services);
        AddConsumers(services);

        AddCaching(services, settings.RedisConnectionString);

        return services;
    }

    public static IServiceCollection AddCourierServiceHealthChecks(
        this IServiceCollection services,
        CourierServiceSettings settings)
    {
        services.AddHealthChecks()
            .AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" })
            .AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" })
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka", tags: new[] { "ready" });

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        string connectionString,
        bool useInMemory)
    {
        if (useInMemory)
        {
            services.AddDbContext<CourierDbContext>(options =>
                options.UseInMemoryDatabase("orders_inmem"));
            return;
        }

        services.AddDbContextPool<CourierDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddMessaging(IServiceCollection services)
    {
        // Kafka Event Producer
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        // Ensure Kafka topics exist on startup
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddScoped<IEventInbox, DbEventInbox<CourierDbContext>>();
    }

    private static void AddOutbox(IServiceCollection services, bool useInMemory)
    {
        if (useInMemory)
            return;

        // Outbox processor
        services.AddHostedService(sp =>
            new OutboxProcessor<CourierDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<CourierDbContext>>>(),
                schema: "couriers"));
        services.AddHostedService<OutboxCleanupHostedService<CourierDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<CourierDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<CourierDbContext, ProcessedCommand>>();
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
            new HangfireCommandExecutor<CourierDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<CourierDbContext>(),
                "courier"));
    }

    private static void AddDomainServices(IServiceCollection services)
    {
        services.AddScoped<ICourierRepository, CourierRepository>();
        // Cache for active couriers list
        services.AddSingleton<ICourierActiveCourierListCache, CourierActiveCourierListRedisCache>();
        // Mapper for domain->integration events for courier
        services.AddSingleton<ICourierEventMapper, CourierEventMapper>();
        // gRPC Location Tracking Client
        services.AddScoped<ILocationTrackingClient, LocationTrackingClient>();
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddConsumers(IServiceCollection services)
    {
        // Event Consumer from OrderService
        services.AddSingleton<CourierEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<CourierEventConsumer>>();
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
}
