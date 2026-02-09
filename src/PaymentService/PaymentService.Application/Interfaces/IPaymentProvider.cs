using PaymentService.Application.Models;

namespace PaymentService.Application.Interfaces;

public interface IPaymentProvider
{
    string Name { get; }
    IReadOnlyCollection<string> Aliases { get; }

    Task<StartPaymentResult> StartPayment(
        StartPaymentRequest request,
        CancellationToken ct);

    Task CapturePayment(
        string externalPaymentId,
        long? amountCents,
        string currency,
        CancellationToken ct);

    Task CancelPayment(
        string externalPaymentId,
        CancellationToken ct);

    Task RefundPayment(
        string externalPaymentId,
        long amountCents,
        string currency,
        CancellationToken ct);
    
    Task<PaymentProviderStatus> CheckStatus(
        string externalPaymentId,
        CancellationToken ct);
}
