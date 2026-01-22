using CourierService.Application.Models;

namespace CourierService.Application.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    /// Persist changes staged in the current DbContext (aggregates + outbox) within a transaction.
    /// </summary>
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,CancellationToken ct = default);
}
