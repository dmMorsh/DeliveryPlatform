using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Grpc;
using OrderService.Api.Mappings;
using OrderService.Application.Interfaces;
using OrderService.Application.MediatR;
using OrderService.Application.Services;
using OrderService.Application.Utils;
using OrderService.Infrastructure.Mapping;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Jobs;
using StackExchange.Redis;
using Serilog;
using Shared.Services;
using Shared.Middleware;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Api;
using OrderService.Infrastructure.Caching;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceTelemetry("order-service");

builder.Services.AddGrpc();

builder.UseExtededSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

string? postgresConnectionString = null;
if (useInMemory)
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
    builder.Services.AddDbContext<KitchenDbContext>(options =>
        options.UseInMemoryDatabase("kitchen_inmem"));
}
else
{
    postgresConnectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContextPool<OrderDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));
    builder.Services.AddDbContextPool<KitchenDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));
    
    builder.Services.AddHangfire(config =>
    {
        config.UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        var connectionString = builder.Configuration.GetConnectionString("Hangfire");
        if (!string.IsNullOrWhiteSpace(connectionString))
            config.UsePostgreSqlStorage(c=>c.UseNpgsqlConnection(connectionString));
        else
            config.UsePostgreSqlStorage(c=>c.UseNpgsqlConnection(postgresConnectionString));
    });
    builder.Services.AddHangfireServer();
    
    // Sharding
    builder.Services.AddSingleton<IShardResolver>(sp =>
    {
        var shardCount = builder.Configuration.GetValue<int>("ShardCount", 1);
        return new HashShardResolver(shardCount);
    });
    builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");

builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddSingleton<IOrderIntegrationEventMapper, IntegrationEventMapper>();
// Event Consumer from other services
builder.Services.AddSingleton<OrderEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<OrderEventConsumer>>();
builder.Services.AddScoped<IEventInbox, DbEventInbox<OrderDbContext>>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<KitchenCapacityOptions>(
    builder.Configuration.GetSection("Kitchen"));
builder.Services.Configure<DeliveryZoneOptions>(
    builder.Configuration.GetSection("DeliveryZones"));
builder.Services.AddSingleton<IDeliveryZoneMatcher, DeliveryZoneMatcher>();
builder.Services.AddScoped<IKitchenSlotRepository, KitchenSlotRepository>();

// Register kitchen slot cache: Noop by default, Redis if configured
builder.Services.AddSingleton<IKitchenSlotCache, NoopKitchenSlotCache>();
var redisConn = builder.Configuration.GetValue<string>("Redis:Connection");
IConnectionMultiplexer? redisConnection = null;
if (!string.IsNullOrWhiteSpace(redisConn))
{
    redisConnection = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => redisConnection);
    builder.Services.AddSingleton<IKitchenSlotCache, RedisKitchenSlotCache>();
    builder.Services.AddSingleton<IDistributedRateLimiter>(sp => new DistributedRateLimiter(redisConnection, sp.GetRequiredService<ILogger<DistributedRateLimiter>>(), "order-service"));
}
else
{
    builder.Services.AddSingleton<IDistributedRateLimiter>(sp => new DistributedRateLimiter(ConnectionMultiplexer.Connect("localhost"), sp.GetRequiredService<ILogger<DistributedRateLimiter>>(), "order-service"));
}

// Only run OutboxProcessor when using a real relational DB
if (!useInMemory)
{
    builder.Services.AddHostedService(sp =>
        new OutboxProcessor<OrderDbContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IEventProducer>(),
            sp.GetRequiredService<ILogger<OutboxProcessor<OrderDbContext>>>(),
            schema: "order"));
    builder.Services.AddHostedService<OutboxCleanupHostedService<OrderDbContext, OutboxMessage>>();
    builder.Services.AddHostedService<ProcessedEventCleanupHostedService<OrderDbContext>>();
    builder.Services.AddSingleton<IOrderPaymentTtlJob, OrderPaymentTtlJob>();
    builder.Services.AddSingleton<IOrderAssigningTtlJob, OrderAssigningTtlJob>();
    builder.Services.AddSingleton<IOrderKitchenAcceptanceTtlJob, OrderKitchenAcceptanceTtlJob>();
    builder.Services.AddSingleton<IOrderKitchenDelayJob, OrderKitchenDelayJob>();
}

// Register MediatR handlers from Application assembly
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>))
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

var healthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig
    {
        BootstrapServers = kafkaBrokers
    },
    name: "kafka",
    tags: new[] { "ready" });

if (!useInMemory && !string.IsNullOrWhiteSpace(postgresConnectionString))
{
    healthChecks.AddNpgSql(postgresConnectionString, name: "db", tags: new[] { "ready" });    
    // Outbox lag health check
    healthChecks.AddCheck<OutboxLagHealthCheck>("outbox_lag", tags: new[] { "ready" });
}

// Adaptive throttle service monitors health and adjusts rate limits
if (!useInMemory && redisConnection != null)
{
    builder.Services.AddSingleton<AdaptiveThrottleService>(sp =>
        new AdaptiveThrottleService(
            sp.GetRequiredService<IDistributedRateLimiter>(),
            sp.GetRequiredService<HealthCheckService>(),
            sp.GetRequiredService<ILogger<AdaptiveThrottleService>>(),
            async () => (await sp.GetRequiredService<OrderDbContext>().OutboxMessages.CountAsync())));
    builder.Services.AddHostedService(sp => sp.GetRequiredService<AdaptiveThrottleService>());
}

var app = builder.Build();

MapsterConfig.RegisterMappings();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseDistributedRateLimit();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<OrderGrpcService>();
app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
    var kitchenDbContext = scope.ServiceProvider.GetRequiredService<KitchenDbContext>();
    kitchenDbContext.Database.Migrate();
    Log.Information("Database migration completed for OrderService");
    
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    var paymentEnabled = bool.TryParse(builder.Configuration["Order:PaymentTtlEnabled"], out var ttlEnabled)
        ? ttlEnabled
        : true;
    if (paymentEnabled)
    {
        var cron = builder.Configuration["Order:PaymentTtlCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 3 * * *";
        
        recurringJobManager.AddOrUpdate<IOrderPaymentTtlJob>(
            "order-payment-ttl",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }

    var assigningEnabled = bool.TryParse(builder.Configuration["Order:AssigningTtlEnabled"], out var assignEnabled)
        ? assignEnabled
        : true;
    if (assigningEnabled)
    {
        var cron = builder.Configuration["Order:AssigningTtlCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 5 * * *";

        recurringJobManager.AddOrUpdate<IOrderAssigningTtlJob>(
            "order-assigning-ttl",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }

    var kitchenEnabled = bool.TryParse(builder.Configuration["Order:KitchenAcceptTtlEnabled"], out var kitchenAcceptEnabled)
        ? kitchenAcceptEnabled
        : true;
    if (kitchenEnabled)
    {
        var cron = builder.Configuration["Order:KitchenAcceptTtlCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 6 * * *";

        recurringJobManager.AddOrUpdate<IOrderKitchenAcceptanceTtlJob>(
            "order-kitchen-accept-ttl",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }

    var kitchenDelayEnabled = bool.TryParse(builder.Configuration["Order:KitchenDelayEnabled"], out var kitchenDelayValue)
        ? kitchenDelayValue
        : true;
    if (kitchenDelayEnabled)
    {
        var cron = builder.Configuration["Order:KitchenDelayCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 12 * * *";

        recurringJobManager.AddOrUpdate<IOrderKitchenDelayJob>(
            "order-kitchen-delay",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.UseHangfireDashboard();

app.Run();
