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

namespace DeliveryService.Application.Services;

public class DeliveryEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<DeliveryEventConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private static class OrderStatusIds
    {
        public const int Confirmed = 2;
        public const int Cancelled = 7;
        public const int Failed = 8;
    }

    public DeliveryEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<DeliveryEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "order.events")
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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
                    return true;
                case "order.status.changed":
                    await HandleOrderStatusChanged(json);
                    return true;
                case "order.canceled":
                    await HandleOrderCanceled(json);
                    return true;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", eventType);
            return false;
        }
    }

    private async Task HandleOrderCreated(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CreateDeliveryFromOrderCommand(
            @event.OrderId,
            @event.ClientId,
            @event.FromAddress,
            @event.ToAddress,
            @event.FromLatitude,
            @event.FromLongitude,
            @event.ToLatitude,
            @event.ToLongitude));
    }

    private async Task HandleOrderStatusChanged(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderStatusChangedEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        if (@event.NewStatus == OrderStatusIds.Confirmed)
        {
            await mediator.Send(new StartAssignmentCommand(@event.OrderId));
        }
        else if (@event.NewStatus is OrderStatusIds.Cancelled or OrderStatusIds.Failed)
        {
            await mediator.Send(new CancelDeliveryByOrderCommand(@event.OrderId, "order_status_changed"));
        }
    }

    private async Task HandleOrderCanceled(string json)
    {
        var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (@event == null) return;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CancelDeliveryByOrderCommand(@event.OrderId, "order_canceled"));
    }
}
