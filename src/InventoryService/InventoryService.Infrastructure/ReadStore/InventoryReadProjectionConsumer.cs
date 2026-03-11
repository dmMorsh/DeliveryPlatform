using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace InventoryService.Infrastructure.ReadStore;

public sealed class InventoryReadProjectionConsumer : KafkaEventConsumerBase
{
    public InventoryReadProjectionConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<InventoryReadProjectionConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "inventory-read-projection", "inventory.events")
    {
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var projector = scope.ServiceProvider.GetRequiredService<InventoryReadProjector>();

            switch (eventType)
            {
                case "stock.reserved":
                    var reserved = Deserialize<StockReservedEvent>(json);
                    if (reserved == null) return true;
                    await projector.HandleAsync(reserved, CancellationToken.None);
                    break;
                case "stock.released":
                    var released = Deserialize<StockReleasedEvent>(json);
                    if (released == null) return true;
                    await projector.HandleAsync(released, CancellationToken.None);
                    break;
                case "stock.quantity_changed":
                    var quantityChanged = Deserialize<StockQuantityChangedEvent>(json);
                    if (quantityChanged == null) return true;
                    await projector.HandleAsync(quantityChanged, CancellationToken.None);
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
