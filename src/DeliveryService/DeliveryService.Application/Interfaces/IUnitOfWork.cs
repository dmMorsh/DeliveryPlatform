using Shared.Contracts;

namespace DeliveryService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(IEnumerable<OutboxMessage> outboxMessages, CancellationToken ct = default);
}
