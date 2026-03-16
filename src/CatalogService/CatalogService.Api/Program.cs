using CatalogService.Application.Interfaces;
using CatalogService.Application.MediatR;
using CatalogService.Application.Services;
using CatalogService.Infrastructure.Mapping;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Repositories;
using CatalogService.Infrastructure.Services;
using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceTelemetry("catalog-service"); 
var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");

// Add services to the container.
// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

if (useInMemory)
{
    builder.Services.AddDbContext<CatalogDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    builder.Services.AddDbContextPool<CatalogDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var catalogHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");
if (!useInMemory)
{
    catalogHealthChecks.AddNpgSql(connectionString, name: "db", tags: new[] { "ready" });
}

var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    redisOptions.AbortOnConnectFail = false;
    var mux = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    catalogHealthChecks.AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
}

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IProductIntegrationEventMapper, ProductIntegrationEventMapper>();
builder.Services.AddSingleton<ICatalogMetricsStore, RedisCatalogMetricsStore>();
// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, DbEventInbox<CatalogDbContext>>();
// Event Consumer from OrderService and InventoryService
builder.Services.AddSingleton<CatalogEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<CatalogEventConsumer>>();
// Outbox processor
if (!useInMemory)
{
    builder.Services.AddHostedService(sp =>
        new OutboxProcessor<CatalogDbContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IEventProducer>(),
            sp.GetRequiredService<ILogger<OutboxProcessor<CatalogDbContext>>>(),
            schema: "catalog"));
    builder.Services.AddHostedService<OutboxCleanupHostedService<CatalogDbContext, OutboxMessage>>();
    builder.Services.AddHostedService<ProcessedEventCleanupHostedService<CatalogDbContext>>();
}

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
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CatalogService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
