using InventoryService.Application.Models;

namespace InventoryService.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IStockItemRepository Stock { get; }
    IReservationRepository Reservations { get; }
    
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default);
}
