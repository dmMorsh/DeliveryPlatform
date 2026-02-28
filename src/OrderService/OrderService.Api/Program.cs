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
using OrderService.Application.Models;
using OrderService.Infrastructure.Mapping;
using OrderService.Infrastructure.Outbox;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Inbox;
using OrderService.Infrastructure.Jobs;
using OrderService.Infrastructure.ReadStore;
using Serilog;
using Shared.Services;

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
string? readStoreConnectionString = null;
if (useInMemory)
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
    builder.Services.AddDbContext<OrderReadDbContext>(options =>
        options.UseInMemoryDatabase("orders_read_inmem"));
}
else
{
    postgresConnectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));

    readStoreConnectionString = builder.Configuration.GetConnectionString("OrderReadStore");
    if (string.IsNullOrWhiteSpace(readStoreConnectionString))
        readStoreConnectionString = postgresConnectionString;

    builder.Services.AddDbContext<OrderReadDbContext>(options =>
        options.UseNpgsql(readStoreConnectionString));

    builder.Services.AddHangfire(config =>
    {
        config.UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        var connectionString = builder.Configuration.GetConnectionString("Hangfire");
        if (!string.IsNullOrWhiteSpace(connectionString))
            config.UsePostgreSqlStorage(connectionString);
        else
            config.UsePostgreSqlStorage(postgresConnectionString);
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
builder.Services.AddScoped<IEventInbox, OrderEventInbox>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<KitchenCapacityOptions>(
    builder.Configuration.GetSection("Kitchen"));
builder.Services.Configure<DeliveryZoneOptions>(
    builder.Configuration.GetSection("DeliveryZones"));
builder.Services.AddSingleton<IDeliveryZoneMatcher, DeliveryZoneMatcher>();
builder.Services.AddScoped<IKitchenSlotReadRepository, KitchenSlotReadRepository>();

if (!useInMemory)
{
    var readServices = new ServiceCollection();
    readServices.AddDbContext<OrderReadDbContext>(options => options.UseNpgsql(readStoreConnectionString!));
    readServices.AddScoped<IEventInbox, OrderReadEventInbox>();
    readServices.AddScoped<IOrderReadProjector, OrderReadProjector>();

    var readProvider = readServices.BuildServiceProvider();
    builder.Services.AddSingleton(new ReadStoreScopeFactory(readProvider.GetRequiredService<IServiceScopeFactory>()));
    builder.Services.AddSingleton(readProvider);

    builder.Services.AddSingleton<OrderReadProjectionConsumer>(sp =>
        new OrderReadProjectionConsumer(
            builder.Configuration,
            builder.Environment,
            sp.GetRequiredService<ILogger<OrderReadProjectionConsumer>>(),
            sp.GetRequiredService<ReadStoreScopeFactory>().ScopeFactory,
            sp.GetRequiredService<IEventProducer>()));
    builder.Services.AddHostedService<KafkaEventConsumerHostedService<OrderReadProjectionConsumer>>();
}

// Only run OutboxProcessor when using a real relational DB
if (!useInMemory)
{
    builder.Services.AddHostedService<OutboxProcessor>();
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
builder.AddExtededAuthentication();
builder.Services.AddAuthorization();
builder.AddExtededCors();

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
}

var app = builder.Build();

MapsterConfig.RegisterMappings();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
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
    Log.Information("Database migration completed for OrderService");

    var enabled = bool.TryParse(builder.Configuration["Order:PaymentTtlEnabled"], out var ttlEnabled)
        ? ttlEnabled
        : true;
    if (enabled)
    {
        var cron = builder.Configuration["Order:PaymentTtlCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 3 * * *";

        RecurringJob.AddOrUpdate<IOrderPaymentTtlJob>(
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

        RecurringJob.AddOrUpdate<IOrderAssigningTtlJob>(
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

        RecurringJob.AddOrUpdate<IOrderKitchenAcceptanceTtlJob>(
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

        RecurringJob.AddOrUpdate<IOrderKitchenDelayJob>(
            "order-kitchen-delay",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
