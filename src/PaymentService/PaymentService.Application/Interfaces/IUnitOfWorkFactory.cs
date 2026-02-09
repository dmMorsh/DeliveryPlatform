namespace PaymentService.Application.Interfaces;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create(Guid orderId);
    Task<Guid?> ResolveOrderIdByExternalPaymentId(string externalPaymentId, CancellationToken ct = default);
}
