using System.Text;
using Confluent.Kafka;
using Hangfire;
using Hangfire.PostgreSql;
using InventoryService.Application;
using InventoryService.Application.Interfaces;
using InventoryService.Application.MediatR;
using InventoryService.Application.Services;
using InventoryService.Application.Utils;
using InventoryService.Infrastructure.Hangfire;
using InventoryService.Infrastructure.Inbox;
using InventoryService.Infrastructure.Mapping;
using InventoryService.Infrastructure.Outbox;
using InventoryService.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region Serilog

builder.Host.UseSerilog((ctx, cfg) =>
    cfg
        .MinimumLevel.Information()
        .Filter.ByExcluding(le =>
            le.Level == LogEventLevel.Information 
            && le.Properties.TryGetValue("commandText", out var cmd)
            && cmd.ToString().StartsWith("\"-- OUTBOX_PROCESSOR_POLL"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("../../logs/InventoryService-.log", 
            rollingInterval: RollingInterval.Day, 
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
);

#endregion

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
    builder.Services.AddDbContext<InventoryDbContext>(options =>
        options.UseNpgsql(connectionString));
    // Outbox processor
    builder.Services.AddHostedService<OutboxProcessor>();
    
    // Hangfire
    builder.Services.AddHangfire(config =>
        // config.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true }));
        config.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("PostgreSQL")), 
            new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true })
        );
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<IHangfireCommandExecutor, HangfireCommandExecutor>();
    
    // Sharding
    builder.Services.AddSingleton<IShardResolver>(sp =>
    {
        var shardCount = builder.Configuration.GetValue<int>("ShardCount");
        return new HashShardResolver(shardCount);
    });
    builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");

builder.Services.AddControllers();
builder.Services
    .AddMediatR(typeof(ApplicationMarker).Assembly)
    .AddTransient(typeof(IPipelineBehavior<,>), typeof(HangfireRetryBehavior<,>));
builder.AddServiceTelemetry("inventory-service");
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

builder.Services.AddSingleton<IStockIntegrationEventMapper, StockIntegrationEventMapper>();

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

        var inventoryCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
        policy.WithOrigins(inventoryCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

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
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for InventoryService");
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
