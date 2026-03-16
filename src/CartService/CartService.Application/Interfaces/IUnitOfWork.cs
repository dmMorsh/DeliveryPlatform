using Shared.Contracts;

namespace CartService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct);
}