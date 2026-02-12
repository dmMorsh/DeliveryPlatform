using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Grpc;
using OrderService.Api.Mappings;
using OrderService.Application;
using OrderService.Application.Interfaces;
using OrderService.Application.MediatR;
using OrderService.Application.Services;
using OrderService.Application.Utils;
using OrderService.Infrastructure.Mapping;
using OrderService.Infrastructure.Outbox;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Inbox;
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
if (useInMemory)
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    postgresConnectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));
    
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

builder.Services.AddSingleton<KafkaEventProducer>();
// Only run OutboxProcessor when using a real relational DB
if (!useInMemory) 
    builder.Services.AddHostedService<OutboxProcessor>();

// Register MediatR handlers from Application assembly
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));;

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
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
