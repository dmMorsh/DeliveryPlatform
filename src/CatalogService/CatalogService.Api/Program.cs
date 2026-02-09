using System.Text;
using CatalogService.Application;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Services;
using CatalogService.Infrastructure.Inbox;
using CatalogService.Infrastructure.Mapping;
using CatalogService.Infrastructure.Outbox;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.Repositories;
using Confluent.Kafka;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

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
    var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
    builder.Services.AddDbContext<CatalogDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");

builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();
builder.AddServiceTelemetry("catalog-service");
var catalogHealthChecks = builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");
if (!useInMemory)
{
    var pg = builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty;
    catalogHealthChecks.AddNpgSql(pg, name: "db", tags: new[] { "ready" });
}

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IProductIntegrationEventMapper, ProductIntegrationEventMapper>();
// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
// Ensure Kafka topics exist on startup
builder.Services.AddHostedService<KafkaTopicBootstrapper>();
builder.Services.AddScoped<IEventInbox, CatalogEventInbox>();
// Event Consumer from OrderService and InventoryService
builder.Services.AddSingleton<CatalogEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<CatalogEventConsumer>>();
// Outbox processor
if (!useInMemory)
    builder.Services.AddHostedService<OutboxProcessor>();

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

        var catalogCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
        policy.WithOrigins(catalogCorsOrigins)
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
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CatalogService");
}
else
{
    Log.Information("Using in-memory database; skipping migrations.");
}

app.Run();
