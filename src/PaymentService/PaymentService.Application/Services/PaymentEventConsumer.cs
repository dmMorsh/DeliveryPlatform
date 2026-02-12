using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Commands.ProcessOrderCanceled;
using PaymentService.Application.Models;
using Shared.Contracts.Events;
using Shared.Services;

namespace PaymentService.Application.Services;

/// <summary>
/// Обработчик событий из OrderService для PaymentService
/// Слушает: order.canceled
/// </summary>
public class PaymentEventConsumer : KafkaEventConsumerBase
{
    private new readonly ILogger<PaymentEventConsumer> _logger;

    public PaymentEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<PaymentEventConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IEventProducer producer)
        : base(config, env, logger, scopeFactory, producer, null, "order.events")
    {
        _logger = logger;
    }

    protected override async Task<bool> HandleMessageAsync(string eventType, string json, ConsumeResult<string, string> message)
    {
        try
        {
            _logger.LogInformation("PaymentService received event: {EventType} from topic {Topic}", eventType, message.Topic);

            switch (eventType)
            {
                case "order.created":
                    await HandleOrderCreated(json);
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

        return true;
    }

    private async Task HandleOrderCreated(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCreatedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var currency = string.IsNullOrWhiteSpace(@event.Currency) ? "RUB" : @event.Currency;
            var cmd = new CreatePaymentCommand(new CreatePaymentModel(@event.OrderId, @event.CostCents, currency));
            await mediator.Send(cmd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent");
        }
    }

    private async Task HandleOrderCanceled(string json)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<OrderCanceledEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (@event == null) return;

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessOrderCanceledCommand(@event.OrderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCanceledEvent");
        }
    }
}
