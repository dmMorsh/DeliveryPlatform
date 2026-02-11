using System.Text;
using Confluent.Kafka;
using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.MediatR;
using DeliveryService.Application.Services;
using DeliveryService.Infrastructure.Inbox;
using DeliveryService.Infrastructure.Outbox;
using DeliveryService.Infrastructure.Persistence;
using DeliveryService.Infrastructure.Repositories;
using DeliveryService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .MinimumLevel.Information()
        .Filter.ByExcluding(le =>
            le.Level == LogEventLevel.Information
            && le.Properties.TryGetValue("commandText", out var cmd)
            && cmd.ToString().StartsWith("\"-- INFRA_BACKGROUND_POLL"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("../../logs/DeliveryService-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.AddServiceTelemetry("delivery-service");

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
    builder.Services.AddDbContext<DeliveryDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "redis:6379");

builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, DeliveryEventInbox>();
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IDeliveryEventMapper, DeliveryEventMapper>();
builder.Services.AddSingleton<IAssignmentQueue, RedisAssignmentQueue>();

builder.Services.AddHttpClient<ICourierDirectory, CourierDirectoryHttpClient>();

builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.Configure<DeliveryAssignmentOptions>(
    builder.Configuration.GetSection("Delivery:Assignment"));

builder.Services.AddScoped<ILocationTrackingClient>(sp =>
    new LocationTrackingClientImpl(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));

builder.Services.AddSingleton<DeliveryEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<DeliveryEventConsumer>>();

builder.Services.AddHostedService<AssignmentScheduler>();

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        var courierCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
        policy.WithOrigins(courierCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var jwtKey = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Key", "dev-key");
var jwtIssuer = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Issuer", "identity-service");
var jwtAudience = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Audience", "platform-api");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Headers["authorization"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken.ToString().Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var connectionString1 = builder.Configuration.GetConnectionString("PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString1))
    throw new InvalidOperationException("PostgreSQL connection string is required.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString1, name: "db", tags: new[] { "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" })
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka", tags: new[] { "ready" });

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
    var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for DeliveryService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
