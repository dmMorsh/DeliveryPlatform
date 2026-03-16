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

namespace OrderReadService.Api.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderReadServiceApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddMediatR(typeof(ApplicationMarker).Assembly);
        return services;
    }

    public static IServiceCollection AddOrderReadServiceCore(
        this IServiceCollection services,
        OrderReadServiceSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PostgresConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is required.");

        AddData(services, settings.PostgresConnectionString);
        AddReadStore(services);
        AddCaching(services, settings.RedisConnectionString);

        return services;
    }

    public static IServiceCollection AddOrderReadServiceHealthChecks(
        this IServiceCollection services,
        OrderReadServiceSettings settings)
    {
        services.AddHealthChecks()
            .AddNpgSql(settings.PostgresConnectionString, name: "db", tags: new[] { "ready" })
            .AddRedis(settings.RedisConnectionString, name: "redis", tags: new[] { "ready" })
            .AddKafka(new ProducerConfig { BootstrapServers = settings.KafkaBrokers }, name: "kafka", tags: new[] { "ready" });

        return services;
    }

    private static void AddData(IServiceCollection services, string connectionString)
    {
        services.AddDbContextPool<OrderReadDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddReadStore(IServiceCollection services)
    {
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<OrderReadProjector>();
        services.AddScoped<IEventInbox, DbEventInbox<OrderReadDbContext>>();
        services.AddSingleton<OrderReadProjectionConsumer>();
        services.AddHostedService<KafkaEventConsumerHostedService<OrderReadProjectionConsumer>>();
    }

    private static void AddCaching(IServiceCollection services, string redisConnectionString)
    {
        // redis cache for order reads
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IOrderReadCache, OrderReadRedisCache>();
    }
}
