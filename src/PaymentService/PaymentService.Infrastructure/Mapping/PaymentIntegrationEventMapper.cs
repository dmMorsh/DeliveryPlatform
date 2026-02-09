using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;
using Shared.Contracts.Events;

namespace PaymentService.Infrastructure.Mapping;

public sealed class PaymentIntegrationEventMapper : IPaymentIntegrationEventMapper
{
    public PaymentCreatedEvent MapCreated(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        AmountCents = payment.AmountCents,
        Currency = payment.Currency,
        Provider = string.IsNullOrWhiteSpace(payment.Provider) ? "unassigned" : payment.Provider,
        ExternalPaymentId = string.IsNullOrWhiteSpace(payment.ExternalPaymentId) ? null : payment.ExternalPaymentId
    };

    public PaymentAuthorizedEvent MapAuthorized(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        AmountCents = payment.AmountCents,
        Currency = payment.Currency,
        Provider = payment.Provider,
        ExternalPaymentId = payment.ExternalPaymentId
    };

    public PaymentCapturedEvent MapCaptured(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        AmountCents = payment.AmountCents,
        Currency = payment.Currency,
        Provider = payment.Provider,
        ExternalPaymentId = payment.ExternalPaymentId
    };

    public PaymentFailedEvent MapFailed(Payment payment, string reason) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        Provider = payment.Provider,
        ExternalPaymentId = string.IsNullOrWhiteSpace(payment.ExternalPaymentId) ? null : payment.ExternalPaymentId,
        Reason = reason
    };

    public PaymentCancelledEvent MapCancelled(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        Provider = payment.Provider,
        ExternalPaymentId = string.IsNullOrWhiteSpace(payment.ExternalPaymentId) ? null : payment.ExternalPaymentId
    };

    public PaymentRefundedEvent MapRefunded(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.OrderId,
        AmountCents = payment.AmountCents,
        Currency = payment.Currency,
        Provider = payment.Provider,
        ExternalPaymentId = payment.ExternalPaymentId
    };
}
