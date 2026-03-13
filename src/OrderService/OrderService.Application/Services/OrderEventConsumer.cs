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
using OrderService.Domain.Aggregates;
using Shared.Contracts.Events;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Services;

/// <summary>
/// Обработчик событий из других сервисов для OrderService
/// Слушает: payment.*, stock.*, delivery.*
/// </summary>
public class OrderEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<OrderEventConsumer> _logger;

    public OrderEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<OrderEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null,null,
            "courier.events", "inventory.events", "payment.events", "delivery.events")
    {
        _logger = logger;
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
        catch (NonRetryableException)
        {
            throw;
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
        var @event = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid PaymentAuthorizedEvent payload");

        await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Confirmed, "payment.authorized");
    }

    private async Task HandlePaymentCaptured(string json)
    {
        var @event = JsonSerializer.Deserialize<PaymentCapturedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid PaymentCapturedEvent payload");

        await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Confirmed, "payment.captured");
    }

    private async Task HandlePaymentFailed(string json)
    {
        var @event = JsonSerializer.Deserialize<PaymentFailedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid PaymentFailedEvent payload");

        await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Failed, "payment.failed");
    }

    private async Task HandlePaymentCancelled(string json)
    {
        var @event = JsonSerializer.Deserialize<PaymentCancelledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid PaymentCancelledEvent payload");

        await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Cancelled, "payment.cancelled");
    }

    private async Task HandlePaymentRefunded(string json)
    {
        var @event = JsonSerializer.Deserialize<PaymentRefundedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid PaymentRefundedEvent payload");

        await UpdateOrderStatusFromPayment(@event.OrderId, OrderStatus.Cancelled, "payment.refunded");
    }

    private async Task UpdateOrderStatusFromPayment(Guid orderId, OrderStatus newStatus, string reason)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateOrderStatusFromPaymentCommand(orderId, newStatus, reason));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }
    private async Task HandleStockReserveFailed(string json)
    {
        var @event = JsonSerializer.Deserialize<StockReserveFailedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid StockReserveFailedEvent payload");

        _logger.LogInformation("📦 OrderService: Reserve failed. OrderId={OrderId}, Items={Items}.",
            @event.OrderId, @event.Items);

        var cmd = new MarkStockReservationFailedCommand(
            @event.OrderId,
            @event.Items.Select(i =>
                new MarkStockFailedItemDto(
                    i.ProductId,
                    i.Quantity,
                    i.Reason
                )).ToArray());

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
            var msg = result.Message ?? string.Join("; ", result.Errors ?? []);
            _logger.LogWarning(
                "⚠️ Failed to change status: OrderId={OrderId}. Error: {Error}", @event.OrderId, msg);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(msg);
            throw new NonRetryableException(msg);
        }
    }

    private async Task HandleStockReserved(string json)
    {
        var @event = JsonSerializer.Deserialize<StockReservedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid StockReservedEvent payload");

        _logger.LogInformation("📦 OrderService: Stock reserved. OrderId={OrderId}, Items={Items}.",
            @event.OrderId, @event.Items);

        var cmd = new UpdateReservedStockCommand(
            @event.OrderId,
            @event.Items.Select(i =>
                new UpdateOrderItemDto(
                    i.ProductId,
                    i.Quantity,
                    null
                )).ToArray());

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
            var msg = result.Message ?? string.Join("; ", result.Errors ?? []);
            _logger.LogWarning(
                "⚠️ Failed to change status: OrderId={OrderId}. Error: {Error}", @event.OrderId, msg);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(msg);
            throw new NonRetryableException(msg);
        }
    }


    private async Task HandleDeliveryAssigned(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryAssignedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryAssignedEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Assigned);
    }

    private async Task HandleDeliveryAccepted(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryAcceptedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryAcceptedEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Assigned);
    }

    private async Task HandleDeliveryPickedUp(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryPickedUpEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryPickedUpEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.InDelivery);
    }

    private async Task HandleDeliveryInTransit(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryInTransitEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryInTransitEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.InDelivery);
    }

    private async Task HandleDeliveryDelivered(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryDeliveredEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryDeliveredEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Delivered);
    }

    private async Task HandleDeliveryCancelled(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryCancelledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryCancelledEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Cancelled);
    }

    private async Task HandleDeliveryFailed(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryFailedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryFailedEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Failed);
    }

    private async Task HandleDeliveryReturned(string json)
    {
        var @event = JsonSerializer.Deserialize<DeliveryReturnedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid DeliveryReturnedEvent payload");

        await UpdateOrderFromDelivery(@event.OrderId, @event.CourierId, OrderStatus.Failed);
    }

    private async Task UpdateOrderFromDelivery(Guid orderId, Guid? courierId, OrderStatus status)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new UpdateOrderCommand(
            orderId,
            courierId,
            null,
            status,
            null
        ));
        if (!result.Success)
        {
            var message = result.Message ?? string.Join("; ", result.Errors ?? []);
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }
}
