using CartService.Application.Models;

namespace CartService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default);
}
