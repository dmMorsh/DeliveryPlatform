using System.Text.Json;
using Confluent.Kafka;
using Shared.Services;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MediatR;
using InventoryService.Application.Commands.ReserveStock;

namespace InventoryService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для InventoryService
/// Слушает: order.created (для резервирования stock)
/// </summary>
public class OrderEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<OrderEventConsumer> _logger;
    private readonly IMediator _mediator;

    public OrderEventConsumer(
        IConfiguration config,
        ILogger<OrderEventConsumer> logger,
        IMediator mediator)
        : base(config, logger, "order.events")
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Обработка входящих событий от OrderService
    /// </summary>
    protected override async Task HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("InventoryService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
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

    private async Task HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 InventoryService: Order created. OrderId={OrderId}. " +
                "🔄 Reserving stock for {ItemCount} items",
                @event.AggregateId, @event.Items?.Count ?? 0);
            
            // Резервируем stock для каждого товара в заказе
            if (@event.Items != null)
            {
                foreach (var item in @event.Items)
                {
                    try
                    {
                        var cmd = new ReserveStockCommand(item.ProductId, @event.OrderId ,item.Quantity);

                        var result = await _mediator.Send(cmd);
                        
                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "✅ Stock reserved: ProductId={ProductId}, Quantity={Quantity}, OrderId={OrderId}",
                                item.ProductId, item.Quantity, @event.AggregateId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "⚠️ Failed to reserve stock: ProductId={ProductId}, OrderId={OrderId}. Error: {Error}",
                                item.ProductId, @event.AggregateId, result.Errors);
                            
                            // TODO: Send event about failed reservation
                            // This would trigger order cancellation in OrderService
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "❌ Error reserving stock for ProductId={ProductId}, OrderId={OrderId}",
                            item.ProductId, @event.AggregateId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }
}

// Extension class for OrderCreatedEvent items
public class OrderItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
