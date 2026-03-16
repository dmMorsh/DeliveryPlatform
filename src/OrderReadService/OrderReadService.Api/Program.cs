using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.MediatR;
using OrderReadService.Infrastructure.Persistence;
using OrderReadService.Infrastructure.Repositories;
using OrderReadService.Infrastructure.Services;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var connectionString = ConfigurationGuard.GetRequiredConnectionString(builder.Configuration, builder.Environment, "PostgreSQL");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("PostgreSQL connection string is required.");

services.AddDbContextPool<OrderReadDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddScoped<IOrderReadRepository, OrderReadRepository>();
services.AddScoped<OrderReadProjector>();
services.AddScoped<IEventInbox, DbEventInbox<OrderReadDbContext>>();
// services.AddScoped<OrderReadProjectionConsumer>();
// services.AddScoped<IEventConsumer>(sp => sp.GetRequiredService<OrderReadProjectionConsumer>());

builder.Services.AddSingleton<OrderReadProjectionConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<OrderReadProjectionConsumer>>();

services.AddControllers();
services.AddEndpointsApiExplorer();

// Register MediatR handlers from Application assembly
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// redis cache for order reads
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var conn = sp.GetRequiredService<IConfiguration>().GetValue<string>("Redis:Connection")
               ?? "localhost";
    return ConnectionMultiplexer.Connect(conn);
});
services.AddSingleton<IOrderReadCache, OrderReadRedisCache>();

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "db", tags: new[] { "ready" })
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

// Migration
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<OrderReadDbContext>();
dbContext.Database.Migrate();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
