using System.Text.Json;
using Confluent.Kafka;
using Shared.Services;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace OrderService.Application.Services;

/// <summary>
/// Обработчик событий из других сервисов для OrderService
/// Слушает: cart.checked_out, courier.status.changed
/// </summary>
public class OrderEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(
        IConfiguration config,
        ILogger<OrderEventConsumer> logger)
        : base(config, logger, "cart.events", "courier.events")
    {
        _logger = logger;
    }

    /// <summary>
    /// Обработка входящих событий от других сервисов
    /// </summary>
    protected override async Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("OrderService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "cart.checked_out":
                    await HandleCartCheckedOut(json);
                    break;
                case "courier.status.changed":
                    await HandleCourierStatusChanged(json);
                    break;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
        }
    }

    private async Task HandleCartCheckedOut(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<CartCheckedOutEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 OrderService: Cart checked out. CartId={CartId}, CustomerId={CustomerId}. " +
                "🔔 TODO: Create order from cart items", 
                @event.CartId, @event.CustomerId);
            
            // TODO: Implement order creation from cart event
            // This would typically:
            // 1. Query CartService via gRPC to get cart items
            // 2. Validate inventory
            // 3. Create order in OrderService
            // 4. Reserve inventory from InventoryService
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CartCheckedOutEvent");
        }
    }

    private async Task HandleCourierStatusChanged(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<dynamic>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("🚚 OrderService: Courier status changed. Event: {Event}",
                json.Substring(0, Math.Min(100, json.Length)));
            
            // TODO: Handle courier status change
            // Update order status based on courier status
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CourierStatusChangedEvent");
        }
    }
}
