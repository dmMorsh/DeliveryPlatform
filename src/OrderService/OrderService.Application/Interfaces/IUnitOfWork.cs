using OrderService.Application.Models;

namespace OrderService.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IOrderRepository Orders { get; }
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default);
}
