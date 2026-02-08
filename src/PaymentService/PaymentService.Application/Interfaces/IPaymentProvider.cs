using PaymentService.Application.Models;

namespace PaymentService.Application.Interfaces;

public interface IPaymentProvider
{
    string Name { get; }

    Task<StartPaymentResult> StartPayment(
        Guid paymentId,
        long amountCents,
        string currency,
        CancellationToken ct);
    
    Task<PaymentProviderStatus> CheckStatus(
        string externalPaymentId,
        CancellationToken ct);
}
