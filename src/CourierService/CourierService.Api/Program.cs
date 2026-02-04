using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CourierService.Application;
using CourierService.Application.Interfaces;
using CourierService.Application.Mapping;
using CourierService.Application.Services;
using CourierService.Infrastructure.Outbox;
using CourierService.Infrastructure.Persistence;
using CourierService.Infrastructure.Repositories;
using Shared.Services;
using MediatR;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .MinimumLevel.Information()
        .Filter.ByExcluding(le =>
            le.Level == LogEventLevel.Information 
            && le.Properties.TryGetValue("commandText", out var cmd)
            && cmd.ToString().StartsWith("\"-- OUTBOX_PROCESSOR_POLL"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/courierservice-YYYY-MM-DD.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory)
{
    builder.Services.AddDbContext<CourierDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
                           ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
    builder.Services.AddDbContext<CourierDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();

// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
// Outbox processor
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddScoped<ICourierRepository, CourierRepository>();
// Mapper for domain->integration events for courier
// builder.Services.AddSingleton<ICourierIntegrationEventMapper, CourierEventMapper>();
builder.Services.AddSingleton<ICourierEventMapper, CourierEventMapper>();
// gRPC Location Tracking Client
builder.Services.AddScoped<ILocationTrackingClient>(sp => 
    new LocationTrackingClientImpl(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));
// Event Consumer from OrderService
builder.Services.AddSingleton<CourierEventConsumer>();
// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var connectionString1 = builder.Configuration.GetConnectionString("PostgreSQL") 
                        ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString1)
    .AddRedis("redis:6379",
        name: "redis")
    .AddKafka(new ProducerConfig
        {
            BootstrapServers = "kafka:9092"
        },
        name: "kafka");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

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

// Start Kafka consumer in background
var consumer = app.Services.GetRequiredService<CourierEventConsumer>();
var cts = new CancellationTokenSource();

_ = Task.Run(async () =>
{
    Log.Information("Starting CourierService Kafka consumer (listening to order.events)...");
    await consumer.StartConsumingAsync(cts.Token);
}, cts.Token);

// Register shutdown handler
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Stopping CourierService consumer...");
    cts.Cancel();
});

app.Run();
