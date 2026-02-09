using System.Text;
using Confluent.Kafka;
using CourierService.Application;
using CourierService.Application.Interfaces;
using CourierService.Application.Mapping;
using CourierService.Application.Services;
using CourierService.Infrastructure.Inbox;
using CourierService.Infrastructure.Outbox;
using CourierService.Infrastructure.Persistence;
using CourierService.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .MinimumLevel.Information()
        .Filter.ByExcluding(le =>
            le.Level == LogEventLevel.Information 
            && le.Properties.TryGetValue("commandText", out var cmd)
            && cmd.ToString().StartsWith("\"-- OUTBOX_PROCESSOR_POLL"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("../../logs/CourierService-.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.AddServiceTelemetry("courier-service");

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

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "redis:6379");
ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "gRPC:LocationTrackingService:Url");

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
    new LocationTrackingClientImpl(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));
// Event Consumer from OrderService
builder.Services.AddSingleton<CourierEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<CourierEventConsumer>>();
// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

// Auth
var jwtKey = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Key");
var jwtIssuer = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Issuer");
var jwtAudience = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Audience");

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
