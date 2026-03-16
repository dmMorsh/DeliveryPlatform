using CartService.Api;
using CartService.Application.Interfaces;
using CartService.Application.MediatR;
using CartService.Infrastructure.Inbox;
using CartService.Infrastructure.Grpc;
using CartService.Infrastructure.Mapping;
using CartService.Infrastructure.Persistence;
using CartService.Infrastructure.Repositories;
using CartService.Infrastructure.Services;
using Confluent.Kafka;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Contracts;
using Shared.Proto;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");

builder.AddServiceTelemetry("cart-service");

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

if (useInMemory)
{
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    builder.Services.AddDbContextPool<CartDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GrpcAuthHeaderHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
var httpTimeoutSeconds = int.TryParse(builder.Configuration["Http:TimeoutSeconds"], out var httpTimeout)
    ? httpTimeout
    : 10;

// Redis for cart caching
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    redisOptions.AbortOnConnectFail = false;
    var mux = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var cartHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");
if (!useInMemory)
{
    cartHealthChecks.AddNpgSql(connectionString, name: "db", tags: new[] { "ready" });
    cartHealthChecks.AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });
}
// Grpc
builder.Services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
{
    var orderGrpcUrl = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "gRPC:OrderService:Url", "https://localhost:7204");
    o.Address = new Uri(orderGrpcUrl);
})
    .AddHttpMessageHandler<GrpcAuthHeaderHandler>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(
            sp.GetRequiredService<ILogger<OrderGrpc.OrderGrpcClient>>(),
            httpTimeoutSeconds));

builder.Services.AddScoped<IOrderService, OrderGrpcService>();

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, CartEventInbox>();

// Cart DDD services
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartReadRepository, CartReadRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ICartIntegrationEventMapper, CartIntegrationEventMapper>();
builder.Services.AddSingleton<ICartReadCache, CartReadRedisCache>();
builder.Services.Configure<CartReadCacheOptions>(builder.Configuration.GetSection("CartCache"));

// Outbox processor
if (!useInMemory)
{
    builder.Services.AddHostedService(sp =>
        new OutboxProcessor<CartDbContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IEventProducer>(),
            sp.GetRequiredService<ILogger<OutboxProcessor<CartDbContext>>>(),
            schema: "cart"));
    builder.Services.AddHostedService<OutboxCleanupHostedService<CartDbContext, OutboxMessage>>();
    builder.Services.AddHostedService<ProcessedEventCleanupHostedService<CartDbContext>>();
}

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

var app = builder.Build();

//Auth
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CartService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();