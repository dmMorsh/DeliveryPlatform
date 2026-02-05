using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    public IOrderRepository Orders { get; }
    
    private readonly OrderDbContext _db;

    public UnitOfWork(OrderDbContext db)
    {
        _db = db;
        Orders = new OrderRepository(_db);
    }


    public async Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default)
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
