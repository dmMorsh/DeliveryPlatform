using OrderService.Application;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _db;

    public UnitOfWork(OrderDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default)
    {
        if (outboxMessages.Count > 0)
            _db.OutboxMessages.AddRange(outboxMessages);

        await _db.SaveChangesAsync(ct);
    }
}
