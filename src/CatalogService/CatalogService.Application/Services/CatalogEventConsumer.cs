using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace CatalogService.Application.Services;

/// <summary>
/// Обработчик событий для CatalogService
/// Слушает: order.created (для обновления популярности), stock.reserved
/// </summary>
public class CatalogEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<CatalogEventConsumer> _logger;

    public CatalogEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<CatalogEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "order.events", "inventory.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService и InventoryService
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CatalogService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
                    break;
                case "stock.reserved":
                    await HandleStockReserved(json);
                    break;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
            return false;
        }

        return true;
    }

    private async Task HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📊 CatalogService: Order created. OrderId={OrderId}. " +
                "📈 TODO: Update product popularity/sales metrics", 
                @event.AggregateId);
            
            // TODO: Implement popularity/sales metrics update
            // This would typically:
            // 1. Extract products from order items
            // 2. Increment sales count for each product
            // 3. Update trending products list
            // 4. Update product recommendations
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }

    private async Task HandleStockReserved(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<StockReservedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 CatalogService: Stock reserved. OrderId={OrderId},. " +
                "💾 TODO: Update available quantity cache",
                @event.OrderId);
            
            // TODO: Update product's available quantity cache
            // This would typically:
            // 1. Get product from cache
            // 2. Decrement available quantity
            // 3. Update cache/send cache invalidation
            // 4. Mark product as "low stock" if needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing StockReservedEvent");
        }
    }
}
