using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
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
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentEventConsumer(
        IConfiguration config,
        IHostEnvironment env,
        ILogger<PaymentEventConsumer> logger,
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
            var factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
            var providers = scope.ServiceProvider.GetRequiredService<IPaymentProviderResolver>();
            var mapper = scope.ServiceProvider.GetRequiredService<IPaymentIntegrationEventMapper>();

            await using var uow = factory.Create(@event.OrderId);
            var payment = await uow.Payments.GetByOrderId(@event.OrderId);
            if (payment is null)
                return;

            if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Cancelled or PaymentStatus.Failed)
                return;

            var outbox = new List<OutboxMessage>();
            var prev = payment.Status;

            if (payment.Status == PaymentStatus.Captured)
            {
                if (string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
                    return;

                var provider = providers.Get(payment.Provider);
                await provider.RefundPayment(payment.ExternalPaymentId, payment.AmountCents, payment.Currency, default);
                payment.MarkRefunded();
                if (payment.Status != prev)
                    outbox.Add(OutboxMessage.From(mapper.MapRefunded(payment)));
            }
            else if (payment.Status is PaymentStatus.Authorized or PaymentStatus.Pending)
            {
                if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
                {
                    var provider = providers.Get(payment.Provider);
                    await provider.CancelPayment(payment.ExternalPaymentId, default);
                }

                payment.MarkCancelled();
                if (payment.Status != prev)
                    outbox.Add(OutboxMessage.From(mapper.MapCancelled(payment)));
            }
            else if (payment.Status == PaymentStatus.Created)
            {
                payment.MarkCancelled();
                if (payment.Status != prev)
                    outbox.Add(OutboxMessage.From(mapper.MapCancelled(payment)));
            }

            if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
                await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider);
            await uow.SaveChangesAsync(outbox);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCanceledEvent");
        }
    }
}
