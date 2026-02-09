using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;

namespace CartService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для CartService
/// Слушает: order.created, order.delivered
/// </summary>
public class CartEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<CartEventConsumer> _logger;

    public CartEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<CartEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "order.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("CartService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
                    break;
                case "order.delivered":
                    await HandleOrderDelivered(json);
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

            _logger.LogInformation("📦 CartService: Order created from cart. OrderId={OrderId}, CustomerId={CustomerId}. " +
                "✅ Cart can be cleared/archived", 
                @event.AggregateId, @event.ClientId);
            
            // TODO: Implement cart clearing/archiving after order creation
            // This would typically:
            // 1. Mark cart as "checked out"
            // 2. Archive cart items
            // 3. Create new empty cart for customer
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }

    private async Task HandleOrderDelivered(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<dynamic>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("🎉 CartService: Order delivered. Event: {Event}",
                json.Substring(0, Math.Min(100, json.Length)));
            
            // TODO: Handle order delivery
            // Could trigger recommendation suggestions for next purchase
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderDeliveredEvent");
        }
    }
}
