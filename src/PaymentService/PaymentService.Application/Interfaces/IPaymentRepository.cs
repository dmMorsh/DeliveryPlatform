using PaymentService.Domain.Aggregates;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByOrderId(Guid orderId, CancellationToken ct = default);
    Task<bool> TryMarkStartingAsync(Guid orderId, CancellationToken ct = default);
    Task UpsertExternalPaymentIdMap(
        Guid orderId,
        Guid paymentId,
        string externalPaymentId,
        string provider,
        CancellationToken ct = default);
}
