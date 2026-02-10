using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands.MarkStockReservationFailed;
using OrderService.Application.Commands.UpdateOrderStatusFromPayment;
using OrderService.Application.Commands.UpdateReservedStock;
using OrderService.Application.Commands.UpdateOrder;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using Shared.Contracts.Events;
using Shared.Services;

namespace OrderService.Application.Services;

/// <summary>
/// Обработчик событий из других сервисов для OrderService
/// Слушает: cart.checked_out, courier.status.changed, payment.*
/// </summary>
public class OrderEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<OrderEventConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<OrderEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "courier.events", "inventory.events", "payment.events", "delivery.events")
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Обработка входящих событий от других сервисов
    /// </summary>
    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
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
                case "payment.authorized":
                    await HandlePaymentAuthorized(json);
                    break;
                case "payment.captured":
                    await HandlePaymentCaptured(json);
                    break;
                case "payment.failed":
                    await HandlePaymentFailed(json);
                    break;
                case "payment.cancelled":
                    await HandlePaymentCancelled(json);
                    break;
                case "payment.refunded":
                    await HandlePaymentRefunded(json);
                    break;
                case "delivery.assigned":
                    await HandleDeliveryAssigned(json);
                    break;
                case "delivery.accepted":
                    await HandleDeliveryAccepted(json);
                    break;
                case "delivery.picked_up":
                    await HandleDeliveryPickedUp(json);
                    break;
                case "delivery.in_transit":
                    await HandleDeliveryInTransit(json);
                    break;
                case "delivery.delivered":
                    await HandleDeliveryDelivered(json);
                    break;
                case "delivery.cancelled":
                    await HandleDeliveryCancelled(json);
                    break;
                case "delivery.failed":
                    await HandleDeliveryFailed(json);
                    break;
                case "delivery.returned":
                    await HandleDeliveryReturned(json);
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

    private async Task HandlePaymentAuthorized(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Confirmed, "payment.authorized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentAuthorizedEvent");
        }
    }

    private async Task HandlePaymentCaptured(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentCapturedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Confirmed, "payment.captured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentCapturedEvent");
        }
    }

    private async Task HandlePaymentFailed(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentFailedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Failed, "payment.failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentFailedEvent");
        }
    }

    private async Task HandlePaymentCancelled(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentCancelledEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Cancelled, "payment.cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentCancelledEvent");
        }
    }

    private async Task HandlePaymentRefunded(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentRefundedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Cancelled, "payment.refunded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentRefundedEvent");
        }
    }

    private async Task UpdateOrderStatusFromPayment(Guid orderId, OrderStatus newStatus, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new UpdateOrderStatusFromPaymentCommand(orderId, newStatus, reason));
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

    private async Task HandleDeliveryAssigned(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryAssignedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Assigned);
    }

    private async Task HandleDeliveryAccepted(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryAcceptedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Assigned);
    }

    private async Task HandleDeliveryPickedUp(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryPickedUpEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.InDelivery);
    }

    private async Task HandleDeliveryInTransit(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryInTransitEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.InDelivery);
    }

    private async Task HandleDeliveryDelivered(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryDeliveredEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Delivered);
    }

    private async Task HandleDeliveryCancelled(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryCancelledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Cancelled);
    }

    private async Task HandleDeliveryFailed(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryFailedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Failed);
    }

    private async Task HandleDeliveryReturned(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryReturnedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Failed);
    }

    private async Task UpdateOrderFromDelivery(Guid orderId, Guid? courierId, OrderStatus status)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var model = new UpdateOrderModel
        {
            CourierId = courierId,
            Status = status
        };

        await mediator.Send(new UpdateOrderCommand(orderId, model));
    }
}
