using DeliveryService.Application.Models;

namespace DeliveryService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(IEnumerable<OutboxMessage> outboxMessages, CancellationToken ct = default);
}
