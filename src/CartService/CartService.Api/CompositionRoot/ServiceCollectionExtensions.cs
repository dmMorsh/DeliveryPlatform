using CartService.Application.Interfaces;
using CartService.Application.MediatR;
using CartService.Infrastructure.Grpc;
using CartService.Infrastructure.Mapping;
using CartService.Infrastructure.Persistence;
using CartService.Infrastructure.Repositories;
using CartService.Infrastructure.Services;
using Confluent.Kafka;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Proto;
using Shared.Services;
using StackExchange.Redis;

namespace CartService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        return services;
    }

    public static IServiceCollection AddCartServiceCore(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CartServiceSettings settings)
    {
        AddData(services, settings.PostgresConnectionString, settings.UseInMemory);
        AddGrpcClients(services, settings);
        AddMessaging(services);
        AddDomainServices(services, configuration);
        AddOutbox(services, settings.UseInMemory);
        AddHangfire(services, settings.PostgresConnectionString, environment);
        AddCaching(services, settings.RedisConnectionString);

        return services;
    }

    public static IServiceCollection AddCartServiceHealthChecks(
        this IServiceCollection services,
        CartServiceSettings settings)
    {
        var healthChecks = services.AddHealthChecks()
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka");

        if (!settings.UseInMemory)
        {
            healthChecks.AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" });
            healthChecks.AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" });
        }

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        string connectionString,
        bool useInMemory)
    {
        if (useInMemory)
        {
            services.AddDbContext<CartDbContext>(options =>
                options.UseInMemoryDatabase("orders_inmem"));
            return;
        }

        services.AddDbContextPool<CartDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddGrpcClients(
        IServiceCollection services,
        CartServiceSettings settings)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<GrpcAuthHeaderHandler>();

        services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
        {
            o.Address = new Uri(settings.OrderGrpcUrl);
        })
            .AddHttpMessageHandler<GrpcAuthHeaderHandler>()
            .AddPolicyHandler((sp, _) =>
                HttpResiliencePolicies.CreatePolicyWrap(
                    sp.GetRequiredService<ILogger<OrderGrpc.OrderGrpcClient>>(),
                    settings.HttpTimeoutSeconds));

        services.AddScoped<IOrderService, OrderGrpcService>();
    }

    private static void AddMessaging(IServiceCollection services)
    {
        // Kafka Event Producer
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        // Ensure Kafka topics exist on startup
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddScoped<IEventInbox, DbEventInbox<CartDbContext>>();
    }

    private static void AddDomainServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartReadRepository, CartReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ICartIntegrationEventMapper, CartIntegrationEventMapper>();
        services.AddSingleton<ICartReadCache, CartReadRedisCache>();
        services.Configure<CartReadCacheOptions>(configuration.GetSection("CartCache"));
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

    private static void AddOutbox(IServiceCollection services, bool useInMemory)
    {
        if (useInMemory)
            return;

        // Outbox processor
        services.AddHostedService(sp =>
            new OutboxProcessor<CartDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<CartDbContext>>>(),
                schema: "cart"));
        services.AddHostedService<OutboxCleanupHostedService<CartDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<CartDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<CartDbContext, ProcessedCommand>>();
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
            new HangfireCommandExecutor<CartDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<CartDbContext>(),
                "cart"));
    }
}
