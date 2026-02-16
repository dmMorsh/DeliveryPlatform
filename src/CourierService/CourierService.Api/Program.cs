using Confluent.Kafka;
using CourierService.Application.Interfaces;
using CourierService.Application.MediatR;
using CourierService.Application.Services;
using CourierService.Infrastructure.Inbox;
using CourierService.Infrastructure.Mapping;
using CourierService.Infrastructure.Outbox;
using CourierService.Infrastructure.Persistence;
using CourierService.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceTelemetry("courier-service");

builder.UseExtededSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

if (useInMemory)
{
    builder.Services.AddDbContext<CourierDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContext<CourierDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();

// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, CourierEventInbox>();
// Outbox processor
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddScoped<ICourierRepository, CourierRepository>();
// Mapper for domain->integration events for courier
builder.Services.AddSingleton<ICourierEventMapper, CourierEventMapper>();
// gRPC Location Tracking Client
builder.Services.AddScoped<ILocationTrackingClient>(sp => 
    new LocationTrackingClient(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClient>>()));
// Event Consumer from OrderService
builder.Services.AddSingleton<CourierEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<CourierEventConsumer>>();
// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Auth
builder.AddExtededAuthentication();
builder.Services.AddAuthorization();
builder.AddExtededCors();

var connectionString1 = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString1))
    throw new InvalidOperationException("PostgreSQL connection string is required.");

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString1, name: "db", tags: new[] { "ready" })
    .AddRedis(redisConnection,
        name: "redis",
        tags: new[] { "ready" })
    .AddKafka(new ProducerConfig
        {
            BootstrapServers = kafkaBrokers
        },
        name: "kafka",
        tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
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
    var dbContext = scope.ServiceProvider.GetRequiredService<CourierDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CourierService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
