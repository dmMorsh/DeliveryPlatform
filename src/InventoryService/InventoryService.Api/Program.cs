using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using InventoryService.Application.Interfaces;
using InventoryService.Application.MediatR;
using InventoryService.Application.Services;
using InventoryService.Application.Utils;
using InventoryService.Application.Models;
using InventoryService.Application.Read;
using InventoryService.Infrastructure.Hangfire;
using InventoryService.Infrastructure.Inbox;
using InventoryService.Infrastructure.Mapping;
using InventoryService.Infrastructure.Outbox;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Jobs;
using InventoryService.Infrastructure.ReadStore;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceTelemetry("inventory-service");

builder.UseExtededSerilog();

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

if (useInMemory)
{
    builder.Services.AddDbContext<InventoryDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
    builder.Services.AddScoped<IUnitOfWorkFactory, MemUnitOfWorkFactory>();
}
else
{   // DbContext
    // Для OutboxProcessor-а и Hangfire
    var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContextPool<InventoryDbContext>(options =>
        options.UseNpgsql(connectionString));
    // Outbox processor
    builder.Services.AddHostedService<OutboxProcessor>();
    builder.Services.AddHostedService<OutboxCleanupHostedService<InventoryDbContext, OutboxMessage>>();
    builder.Services.AddHostedService<ProcessedEventCleanupHostedService<InventoryDbContext>>();
    builder.Services.AddHostedService<ProcessedCommandCleanupHostedService<InventoryDbContext, ProcessedCommand>>();
    
    // Hangfire
    builder.Services.AddHangfire(config =>
        // config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true }));
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("PostgreSQL")), 
            new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true })
        );
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<IHangfireCommandExecutor, HangfireCommandExecutor>();
    builder.Services.AddSingleton<IInventoryReservationAlertJob, InventoryReservationAlertJob>();
    
    // Sharding
    builder.Services.AddSingleton<IShardResolver>(sp =>
    {
        var shardCount = builder.Configuration.GetValue<int>("ShardCount");
        return new HashShardResolver(shardCount);
    });
    builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
}

builder.Services.AddControllers();
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

if (!useInMemory)
{
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");

var inventoryHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");
if (!useInMemory)
{
    var pg = builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty;
    inventoryHealthChecks.AddNpgSql(pg, name: "db", tags: new[] { "ready" });
}

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
// Event Consumer from OrderService
builder.Services.AddSingleton<InventoryEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<InventoryEventConsumer>>();
builder.Services.AddScoped<IEventInbox, InventoryEventInbox>();

// read‑store context for inventory
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    redisOptions.AbortOnConnectFail = false;
    var mux = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    inventoryHealthChecks.AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
}

builder.Services.AddScoped<IInventoryReadCache, InventoryReadRedisCache>();
builder.Services.AddDbContextPool<InventoryReadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
builder.Services.AddScoped<IInventoryReadRepository, InventoryReadRepository>();
builder.Services.AddScoped<InventoryReadProjector>();

builder.Services.AddSingleton<InventoryReadProjectionConsumer>(sp =>
    new InventoryReadProjectionConsumer(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<InventoryReadProjectionConsumer>>(),
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<IEventProducer>()
    ));
builder.Services.AddHostedService<KafkaEventConsumerHostedService<InventoryReadProjectionConsumer>>();

builder.Services.AddSingleton<IStockIntegrationEventMapper, StockIntegrationEventMapper>();

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
    var inventoryReadDbContext = scope.ServiceProvider.GetRequiredService<InventoryReadDbContext>();
    inventoryReadDbContext.Database.Migrate();
    Log.Information("Database migration completed for InventoryService");
    
    var enabled = bool.TryParse(builder.Configuration["Inventory:ReservationAlertEnabled"], out var alertEnabled)
        ? alertEnabled
        : true;
    if (enabled)
    {
        var cron = builder.Configuration["Inventory:ReservationAlertCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 4 * * *";

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<IInventoryReservationAlertJob>(
            "inventory-reservation-alert",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.Run();
