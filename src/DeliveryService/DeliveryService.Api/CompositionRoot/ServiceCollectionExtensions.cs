using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DeliveryService.Application.Interfaces;
using DeliveryService.Application.MediatR;
using DeliveryService.Application.Services;
using DeliveryService.Infrastructure.Jobs;
using DeliveryService.Infrastructure.Mapping;
using DeliveryService.Infrastructure.Persistence;
using DeliveryService.Infrastructure.Repositories;
using DeliveryService.Infrastructure.Services;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace DeliveryService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        return services;
    }

    public static IServiceCollection AddDeliveryServiceCore(
        this IServiceCollection services,
        IConfiguration configuration,
        DeliveryServiceSettings settings)
    {
        AddData(services, configuration, settings);
        AddMessaging(services);
        AddOutboxAndJobs(services, settings.UseInMemory);
        AddDomainServices(services, configuration, settings.HttpTimeoutSeconds);
        AddConsumers(services);

        AddCaching(services, settings.RedisConnectionString);

        return services;
    }

    public static IServiceCollection AddDeliveryServiceHealthChecks(
        this IServiceCollection services,
        DeliveryServiceSettings settings)
    {
        var connectionString = settings.PostgresConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "db", tags: new[] { "ready" })
            .AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" })
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka", tags: new[] { "ready" });

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        IConfiguration configuration,
        DeliveryServiceSettings settings)
    {
        if (settings.UseInMemory)
        {
            services.AddDbContext<DeliveryDbContext>(options =>
                options.UseInMemoryDatabase("delivery_inmem"));
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");

        var connectionString = settings.PostgresConnectionString;
        services.AddDbContextPool<DeliveryDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            var hangfireConnection = configuration.GetConnectionString("Hangfire");
            if (!string.IsNullOrWhiteSpace(hangfireConnection))
                config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnection));
            else
                config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
        });
        services.AddHangfireServer();

        services.AddScoped<IHangfireCommandExecutor>(sp =>
            new HangfireCommandExecutor<DeliveryDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<DeliveryDbContext>(),
                "delivery"));
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddScoped<IEventInbox, DbEventInbox<DeliveryDbContext>>();
    }

    private static void AddOutboxAndJobs(IServiceCollection services, bool useInMemory)
    {
        if (useInMemory)
            return;

        services.AddHostedService(sp =>
            new OutboxProcessor<DeliveryDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<DeliveryDbContext>>>(),
                schema: "delivery"));
        services.AddHostedService<OutboxCleanupHostedService<DeliveryDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<DeliveryDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<DeliveryDbContext, ProcessedCommand>>();
        services.AddSingleton<IDeliverySlaJob, DeliverySlaJob>();
    }

    private static void AddDomainServices(
        IServiceCollection services,
        IConfiguration configuration,
        int httpTimeoutSeconds)
    {
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDeliveryEventMapper, DeliveryEventMapper>();
        services.AddSingleton<IAssignmentQueue, RedisAssignmentQueue>();
        services.AddSingleton<IDeliveryOfferCache, DeliveryOfferRedisCache>();

        services.AddHttpClient<ICourierDirectory, CourierDirectoryHttpClient>()
            .AddPolicyHandler((sp, _) =>
                HttpResiliencePolicies.CreatePolicyWrap(
                    sp.GetRequiredService<ILogger<CourierDirectoryHttpClient>>(),
                    httpTimeoutSeconds));

        services.AddScoped<IAssignmentService, AssignmentService>();
        services.Configure<DeliveryAssignmentOptions>(
            configuration.GetSection("Delivery:Assignment"));
        services.Configure<DeliveryEtaOptions>(
            configuration.GetSection("Delivery:Eta"));
        services.AddSingleton<IDeliveryEtaCalculator, DeliveryEtaCalculator>();
        services.Configure<CourierAvailabilityOptions>(
            configuration.GetSection("Delivery:Courier"));
        services.AddSingleton<ICourierActivityStore, CourierActivityRedisStore>();

        services.AddScoped<ILocationTrackingClient, LocationTrackingClient>();
    }

    private static void AddConsumers(IServiceCollection services)
    {
        services.AddSingleton<DeliveryEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<DeliveryEventConsumer>>();
        services.AddHostedService<AssignmentScheduler>();
    }

    private static void AddCaching(
        IServiceCollection services,
        string redisConnectionString)
    {
        try
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            var mux = ConnectionMultiplexer.Connect(redisOptions);
            services.AddSingleton<IConnectionMultiplexer>(mux);
        }
        catch (Exception ex)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                throw new InvalidOperationException($"Failed to connect to Redis at {redisConnectionString}", ex));
        }
    }
}
