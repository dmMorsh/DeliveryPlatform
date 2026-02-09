namespace PaymentService.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IPaymentRepository Payments { get; }
    Task SaveChangesAsync(CancellationToken ct = default);
    Task SaveChangesAsync(List<Models.OutboxMessage> outboxMessages, CancellationToken ct = default);
}
