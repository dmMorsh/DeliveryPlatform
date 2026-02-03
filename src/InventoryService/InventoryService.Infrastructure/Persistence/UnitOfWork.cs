using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Infrastructure.Repositories;

namespace InventoryService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    public IStockItemRepository Stock { get; }
    public IReservationRepository Reservations { get; }
    
    private readonly InventoryDbContext _db;

    public UnitOfWork(InventoryDbContext db)
    {
        _db = db;
        Stock = new StockItemRepository(_db);
        Reservations = new ReservationRepository(_db);
    }
    
    public async Task SaveChangesAsync(List<OutboxMessage> outboxMessages, CancellationToken ct = default)
    {
        if (outboxMessages.Count > 0)
            _db.OutboxMessages.AddRange(outboxMessages);

        await _db.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}