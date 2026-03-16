using System.Threading.RateLimiting;
using Confluent.Kafka;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Security;
using PaymentService.Application.Interfaces;
using PaymentService.Application.MediatR;
using PaymentService.Application.Services;
using PaymentService.Infrastructure.Jobs;
using PaymentService.Infrastructure.Mapping;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Providers;
using PaymentService.Infrastructure.Sharding;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

namespace PaymentService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddGrpc();
        services.AddOpenApi();
        return services;
    }

    public static IServiceCollection AddPaymentServiceCore(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        PaymentServiceSettings settings)
    {
        AddData(services, settings);
        AddCaching(services, settings.RedisConnectionString);
        AddMediatR(services);
        AddDomainServices(services, configuration, settings.HttpTimeoutSeconds);
        AddMessaging(services);
        AddOutbox(services);
        AddHangfire(services, environment, settings.HangfireConnectionString);

        return services;
    }

    public static IServiceCollection AddPaymentServiceHealthChecks(
        this IServiceCollection services,
        PaymentServiceSettings settings)
    {
        var healthChecks = services.AddHealthChecks()
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka");

        healthChecks.AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" });

        return services;
    }

    public static IServiceCollection AddPaymentRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("payment-default", httpContext =>
            {
                var key = httpContext.User?.Identity?.IsAuthenticated == true
                    ? $"user:{httpContext.User.Identity!.Name ?? httpContext.User.FindFirst("sub")?.Value ?? "unknown"}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 60,
                    TokensPerPeriod = 60,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }

    private static void AddData(
        IServiceCollection services,
        PaymentServiceSettings settings)
    {
        services.AddDbContextPool<PaymentDbContext>(options =>
            options.UseNpgsql(settings.PostgresConnectionString));
        services.AddDbContextPool<PaymentShardMapDbContext>(options =>
            options.UseNpgsql(settings.PostgresConnectionString));
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

    private static void AddMediatR(IServiceCollection services)
    {
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddScoped<IHangfireCommandExecutor>(sp =>
            new HangfireCommandExecutor<PaymentDbContext>(
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<PaymentDbContext>(),
                "payment"));
    }

    private static void AddDomainServices(
        IServiceCollection services,
        IConfiguration configuration,
        int httpTimeoutSeconds)
    {
        services.AddSingleton<IPaymentStatusCache, PaymentStatusRedisCache>();
        services.AddSingleton<IPaymentDbContextFactory, PaymentDbContextFactory>();
        services.Configure<PaymentShardOptions>(configuration.GetSection("Payments:Sharding"));
        services.AddSingleton<IPaymentShardRouter, PaymentShardRouter>();
        services.Configure<PaymentShardMapOptions>(configuration.GetSection("Payments:ShardMap"));
        services.AddSingleton<IPaymentShardMapDbContextFactory, PaymentShardMapDbContextFactory>();
        services.Configure<SberbankOptions>(configuration.GetSection("Payments:Sberbank"));
        services.Configure<YooMoneyOptions>(configuration.GetSection("Payments:YooMoney"));
        services.Configure<FakePaymentOptions>(configuration.GetSection("Payments:FakeProvider"));
        services.Configure<PaymentStatusCheckOptions>(configuration.GetSection("Payments:StatusCheck"));
        services.Configure<WebhookOptions>(configuration.GetSection("Payments:Webhooks"));

        services.AddHttpClient<SberbankPaymentProvider>()
            .AddPolicyHandler((sp, _) =>
                HttpResiliencePolicies.CreatePolicyWrap(
                    sp.GetRequiredService<ILogger<SberbankPaymentProvider>>(),
                    httpTimeoutSeconds));
        services.AddHttpClient<YooMoneyPaymentProvider>()
            .AddPolicyHandler((sp, _) =>
                HttpResiliencePolicies.CreatePolicyWrap(
                    sp.GetRequiredService<ILogger<YooMoneyPaymentProvider>>(),
                    httpTimeoutSeconds));
        services.AddHttpClient<FakePaymentProvider>()
            .AddPolicyHandler((sp, _) =>
                HttpResiliencePolicies.CreatePolicyWrap(
                    sp.GetRequiredService<ILogger<FakePaymentProvider>>(),
                    httpTimeoutSeconds));

        services.AddScoped<IPaymentProvider, SberbankPaymentProvider>();
        services.AddScoped<IPaymentProvider, YooMoneyPaymentProvider>();
        services.AddScoped<IPaymentProvider, FakePaymentProvider>();
        services.AddScoped<IPaymentProviderResolver, PaymentProviderResolver>();
        services.AddScoped<IPaymentStatusCheckScheduler, PaymentStatusCheckScheduler>();
        services.AddSingleton<IWebhookValidator, WebhookValidator>();
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddSingleton<IEventProducer, KafkaEventProducer>();
        services.AddHostedService<KafkaTopicBootstrapper>();
        services.AddSingleton<PaymentEventConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<PaymentEventConsumer>>();
        services.AddScoped<IEventInbox, DbEventInbox<PaymentDbContext>>();
        services.AddSingleton<IPaymentIntegrationEventMapper, PaymentIntegrationEventMapper>();
    }

    private static void AddOutbox(IServiceCollection services)
    {
        services.AddHostedService(sp =>
            new OutboxProcessor<PaymentDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IEventProducer>(),
                sp.GetRequiredService<ILogger<OutboxProcessor<PaymentDbContext>>>(),
                schema: "payment",
                defaultTopic: "payment.events"));
        services.AddHostedService<OutboxCleanupHostedService<PaymentDbContext, OutboxMessage>>();
        services.AddHostedService<ProcessedEventCleanupHostedService<PaymentDbContext>>();
        services.AddHostedService<ProcessedCommandCleanupHostedService<PaymentDbContext, ProcessedCommand>>();
    }

    private static void AddHangfire(
        IServiceCollection services,
        IHostEnvironment environment,
        string? hangfireConnectionString)
    {
        services.AddHangfire(config =>
        {
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
                config.UsePostgreSqlStorage(hangfireConnectionString);
            else
            {
                if (environment.IsProduction())
                    throw new InvalidOperationException("Hangfire connection string is required in production.");
                config.UseMemoryStorage();
            }
        });
        services.AddHangfireServer();
    }
}
