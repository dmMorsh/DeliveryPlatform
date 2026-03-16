using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using DeliveryService.Application.Interfaces;
using DeliveryService.Application.MediatR;
using DeliveryService.Application.Services;
using DeliveryService.Infrastructure.Inbox;
using DeliveryService.Infrastructure.Mapping;
using DeliveryService.Infrastructure.Persistence;
using DeliveryService.Infrastructure.Repositories;
using DeliveryService.Infrastructure.Services;
using DeliveryService.Infrastructure.Jobs;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Contracts;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceTelemetry("delivery-service")
    .WithMetrics(m => m.AddMeter("DeliveryService.Assignment"));

builder.UseExtededSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
var httpTimeoutSeconds = int.TryParse(builder.Configuration["Http:TimeoutSeconds"], out var httpTimeout)
    ? httpTimeout
    : 10;

var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

if (useInMemory)
{
    builder.Services.AddDbContext<DeliveryDbContext>(options =>
        options.UseInMemoryDatabase("delivery_inmem"));
}
else
{
    var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContextPool<DeliveryDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddHangfire(config =>
    {
        config.UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire");
        if (!string.IsNullOrWhiteSpace(hangfireConnection))
            config.UsePostgreSqlStorage(c=>c.UseNpgsqlConnection(hangfireConnection));
        else
            config.UsePostgreSqlStorage(c=>c.UseNpgsqlConnection(connectionString));
    });
    builder.Services.AddHangfireServer();
}

builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, DeliveryEventInbox>();
if (!useInMemory)
{
    builder.Services.AddHostedService(sp =>
        new OutboxProcessor<DeliveryDbContext>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IEventProducer>(),
            sp.GetRequiredService<ILogger<OutboxProcessor<DeliveryDbContext>>>(),
            schema: "delivery"));
    builder.Services.AddHostedService<OutboxCleanupHostedService<DeliveryDbContext, OutboxMessage>>();
    builder.Services.AddHostedService<ProcessedEventCleanupHostedService<DeliveryDbContext>>();
    builder.Services.AddSingleton<IDeliverySlaJob, DeliverySlaJob>();
}

builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IDeliveryEventMapper, DeliveryEventMapper>();
builder.Services.AddSingleton<IAssignmentQueue, RedisAssignmentQueue>();
builder.Services.AddSingleton<IDeliveryOfferCache, DeliveryOfferRedisCache>();

builder.Services.AddHttpClient<ICourierDirectory, CourierDirectoryHttpClient>()
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(
            sp.GetRequiredService<ILogger<CourierDirectoryHttpClient>>(),
            httpTimeoutSeconds));

builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.Configure<DeliveryAssignmentOptions>(
    builder.Configuration.GetSection("Delivery:Assignment"));
builder.Services.Configure<DeliveryEtaOptions>(
    builder.Configuration.GetSection("Delivery:Eta"));
builder.Services.AddSingleton<IDeliveryEtaCalculator, DeliveryEtaCalculator>();
builder.Services.Configure<CourierAvailabilityOptions>(
    builder.Configuration.GetSection("Delivery:Courier"));
builder.Services.AddSingleton<ICourierActivityStore, CourierActivityRedisStore>();

builder.Services.AddScoped<ILocationTrackingClient, LocationTrackingClient>();

builder.Services.AddSingleton<DeliveryEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<DeliveryEventConsumer>>();

builder.Services.AddHostedService<AssignmentScheduler>();

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
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

var connectionString1 = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString1))
    throw new InvalidOperationException("PostgreSQL connection string is required.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString1, name: "db", tags: new[] { "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" })
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka", tags: new[] { "ready" });

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for DeliveryService");

    var enabled = bool.TryParse(builder.Configuration["Delivery:SlaEnabled"], out var slaEnabled)
        ? slaEnabled
        : true;
    if (enabled)
    {
        var cron = builder.Configuration["Delivery:SlaCron"];
        if (string.IsNullOrWhiteSpace(cron))
            cron = "0 8,20 * * *";

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<IDeliverySlaJob>(
            "delivery-sla",
            job => job.ExecuteAsync(CancellationToken.None),
            cron);
    }
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
