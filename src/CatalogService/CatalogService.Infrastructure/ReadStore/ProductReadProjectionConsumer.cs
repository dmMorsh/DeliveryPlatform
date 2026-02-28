using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace CatalogService.Infrastructure.ReadStore;

public sealed class ProductReadProjectionConsumer : KafkaEventConsumerBase
{
    private readonly ILogger<ProductReadProjectionConsumer> _logger;

    public ProductReadProjectionConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<ProductReadProjectionConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "product.events")
    {
        _logger = logger;
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var projector = scope.ServiceProvider.GetRequiredService<ProductReadProjector>();

            switch (eventType)
            {
                case "product.created":
                    var created = Deserialize<ProductCreatedEvent>(json);
                    if (created == null) return true;
                    await projector.HandleAsync(created, CancellationToken.None);
                    break;
                case "product.price_changed":
                    var price = Deserialize<ProductPriceChangedEvent>(json);
                    if (price == null) return true;
                    await projector.HandleAsync(price, CancellationToken.None);
                    break;
                default:
                    return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Read projection error for event {EventType}", eventType);
            return false;
        }
    }

    private static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
