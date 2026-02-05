using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands.MarkStockReservationFailed;
using OrderService.Application.Commands.UpdateReservedStock;
using OrderService.Application.Models;
using Shared.Contracts.Events;
using Shared.Services;

namespace OrderService.Application.Services;

/// <summary>
/// Обработчик событий из других сервисов для OrderService
/// Слушает: cart.checked_out, courier.status.changed
/// </summary>
public class OrderEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<OrderEventConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderEventConsumer(
        IConfiguration config,
        ILogger<OrderEventConsumer> logger, IServiceScopeFactory scopeFactory)
        : base(config, logger, "courier.events", "inventory.events")
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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
                    await _HandleCartCheckedOut(json);
                    break;
                case "courier.status.changed":
                    await HandleCourierStatusChanged(json);
                    break;
                case "stock.reserved":
                    await HandleStockReserved(json);
                    break;
                case "stock.reserve_failed":
                    await HandleStockReserveFailed(json);
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

    private async Task HandleStockReserveFailed(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<StockReserveFailedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 OrderService: Reserve failed. OrderId={OrderId}, Items={Items}.", 
                @event.OrderId, @event.Items);
            
            var cmd = new MarkStockReservationFailedCommand(@event.OrderId,
                new UpdateOrderItemsModel(ItemModeStatus.ReservationFailed, @event.Items.Select(i =>
                    new UpdateOrderItemModel(
                        i.ProductId,
                        i.Quantity,
                        i.Reason
                    )).ToArray()));
            
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(cmd);
                    
            if (result.Success)
            {
                _logger.LogInformation(
                    "✅ Status changed to Failed : OrderId={OrderId}", @event.OrderId);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ Failed to change status: OrderId={OrderId}. Error: {Error}", @event.OrderId, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing StockReserveFailedEvent");
        }
    }

    private async Task HandleStockReserved(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<StockReservedEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            _logger.LogInformation("📦 OrderService: Stock reserved. OrderId={OrderId}, Items={Items}.", 
                @event.OrderId, @event.Items);

            var cmd = new UpdateReservedStockCommand(@event.OrderId,
                new UpdateOrderItemsModel(ItemModeStatus.Reserved, @event.Items.Select(i =>
                    new UpdateOrderItemModel(
                        i.ProductId,
                        i.Quantity,
                        null
                    )).ToArray()));
            
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(cmd);
                    
            if (result.Success)
            {
                _logger.LogInformation(
                    "✅ Status changed to Reserved : OrderId={OrderId}", @event.OrderId);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ Failed to change status: OrderId={OrderId}. Error: {Error}", @event.OrderId, result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing StockReservedEvent");
        }
    }

    private Task _HandleCartCheckedOut(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<CartCheckedOutEvent>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return Task.CompletedTask;

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

        return Task.CompletedTask;
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