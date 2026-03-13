using System.Text.Json;
using Confluent.Kafka;
using DeliveryService.Application.Commands.CreateDeliveryFromOrder;
using DeliveryService.Application.Commands.StartAssignment;
using DeliveryService.Application.Commands.CancelDelivery;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Services;

public class DeliveryEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<DeliveryEventConsumer> _logger;

    private static class OrderStatusIds
    {
        public const int Confirmed = (int)OrderStatusCode.Confirmed;
        public const int Cancelled = (int)OrderStatusCode.Cancelled;
        public const int Failed = (int)OrderStatusCode.Failed;
    }

    public DeliveryEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DeliveryEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, null,
            "order.events")
    {
        _logger = logger;
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("DeliveryService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
                    break;
                case "order.ready":
                    await HandleOrderReady(json);
                    break;
                case "order.status.changed":
                    await HandleOrderStatusChanged(json);
                    break;
                case "order.canceled":
                    await HandleOrderCanceled(json);
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

    private async Task HandleOrderCreated(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderCreatedEvent payload");

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new CreateDeliveryFromOrderCommand(
            @event.OrderId,
            @event.ClientId,
            @event.FromAddress,
            @event.ToAddress,
            @event.FromLatitude,
            @event.FromLongitude,
            @event.ToLatitude,
            @event.ToLongitude,
            @event.DeliveryZoneId,
            @event.DeliveryZoneName,
            @event.DeliveryPickupSlaMinutes,
            @event.DeliveryTransitSlaMinutes,
            @event.DeliveryFeeMultiplier,
            @event.DeliveryZoneDistanceKm));
        if (!result.Success)
        {
            var message = result.Message ?? "Create delivery failed";
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }

    private async Task HandleOrderStatusChanged(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderStatusChangedEvent payload");

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        if (@event.NewStatus is OrderStatusIds.Cancelled or OrderStatusIds.Failed)
        {
            var result = await mediator.Send(new CancelDeliveryByOrderCommand(@event.OrderId, "order_status_changed"));
            if (!result.Success)
            {
                var message = result.Message ?? "Cancel delivery failed";
                if (result.ErrorCode == ErrorCodes.NotFound)
                    throw new Exception(message);
                throw new NonRetryableException(message);
            }
        }
    }

    private async Task HandleOrderReady(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderReadyEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderReadyEvent payload");

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new StartAssignmentCommand(@event.OrderId));
        if (!result.Success)
        {
            var message = result.Message ?? "Start assignment failed";
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }

    private async Task HandleOrderCanceled(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) throw new NonRetryableException("Invalid OrderCanceledEvent payload");

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new CancelDeliveryByOrderCommand(@event.OrderId, "order_canceled"));
        if (!result.Success)
        {
            var message = result.Message ?? "Cancel delivery failed";
            if (result.ErrorCode == ErrorCodes.NotFound)
                throw new Exception(message);
            throw new NonRetryableException(message);
        }
    }
}
