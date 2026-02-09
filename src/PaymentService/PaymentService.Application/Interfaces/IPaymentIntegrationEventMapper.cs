using PaymentService.Domain.Aggregates;
using Shared.Contracts.Events;

namespace PaymentService.Application.Interfaces;

public interface IPaymentIntegrationEventMapper
{
    PaymentCreatedEvent MapCreated(Payment payment);
    PaymentAuthorizedEvent MapAuthorized(Payment payment);
    PaymentCapturedEvent MapCaptured(Payment payment);
    PaymentFailedEvent MapFailed(Payment payment, string reason);
    PaymentCancelledEvent MapCancelled(Payment payment);
    PaymentRefundedEvent MapRefunded(Payment payment);
}
