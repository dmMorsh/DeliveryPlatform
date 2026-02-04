using System.Text.Json;
using Confluent.Kafka;
using InventoryService.Application.Commands.ReleaseStock;
using Shared.Services;
using Shared.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MediatR;
using InventoryService.Application.Commands.ReserveStock;
using InventoryService.Application.Models;

namespace InventoryService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для InventoryService
/// Слушает: order.created (для резервирования stock)
/// </summary>
public class InventoryEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<InventoryEventConsumer> _logger;
    private readonly IMediator _mediator;

    public InventoryEventConsumer(
        IConfiguration config,
        ILogger<InventoryEventConsumer> logger,
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
                case "order.canceled":
                    await HandleOrderCanceled(json);
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

    private async Task HandleOrderCanceled(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 InventoryService: Order canceled. OrderId={OrderId}. ",
                @event.AggregateId);
            
            // Отменяем резерв stock
            try
            {
                var cmd = new ReleaseStockCommand(@event.OrderId, null);

                var result = await _mediator.Send(cmd);
                
                if (result.Success)
                {
                    _logger.LogInformation(
                        "✅ Stock released: OrderId={OrderId}", @event.AggregateId);
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ Failed to release stock: OrderId={OrderId}. Error: {Error}", @event.AggregateId, result.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "❌ Error releasing stock for OrderId={OrderId}", @event.AggregateId);
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCanceledEvent");
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
                try
                {
                    var cmd = new ReserveStockCommand(@event.OrderId ,@event.Items
                        .Select(i => 
                            new ReserveStockModel(
                                i.ProductId, 
                                i.Quantity)
                        ).ToArray());

                    var result = await _mediator.Send(cmd);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "✅ Stock reserved: OrderId={OrderId}", @event.AggregateId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Failed to reserve stock: OrderId={OrderId}. Error: {Error}", @event.AggregateId, result.Errors);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "❌ Error reserving stock for OrderId={OrderId}", @event.AggregateId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }
}