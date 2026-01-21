using InventoryService.Application.Models;

namespace InventoryService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default);
}
