using CartService.Api;
using CartService.Application.Interfaces;
using CartService.Application.MediatR;
using CartService.Application.Services;
using CartService.Infrastructure.Inbox;
using CartService.Infrastructure.Grpc;
using CartService.Infrastructure.Mapping;
using CartService.Infrastructure.Outbox;
using CartService.Infrastructure.Persistence;
using CartService.Infrastructure.Repositories;
using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Proto;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

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
    var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GrpcAuthHeaderHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var cartHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");
if (!useInMemory)
{
    var pg = builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty;
    cartHealthChecks.AddNpgSql(pg, name: "db", tags: new[] { "ready" });
}

builder.Services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
{
    var orderGrpcUrl = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "gRPC:OrderService:Url", "https://localhost:7204");
    o.Address = new Uri(orderGrpcUrl);
})
    .AddHttpMessageHandler<GrpcAuthHeaderHandler>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILogger<OrderGrpc.OrderGrpcClient>>()));

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
// Event Consumer from OrderService
builder.Services.AddSingleton<CartEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<CartEventConsumer>>();
// Outbox processor
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

// Auth
builder.AddExtededAuthentication();
builder.Services.AddAuthorization();
builder.AddExtededCors();

var app = builder.Build();

//Auth
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
