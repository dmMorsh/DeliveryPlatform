using PaymentService.Application.Models;

namespace PaymentService.Application.Interfaces;

public interface IPaymentStatusCache
{
    Task<PaymentStatusView?> GetAsync(Guid orderId, CancellationToken ct);
    Task SetAsync(Guid orderId, PaymentStatusView view, CancellationToken ct);
    Task RemoveAsync(Guid orderId, CancellationToken ct);
}
