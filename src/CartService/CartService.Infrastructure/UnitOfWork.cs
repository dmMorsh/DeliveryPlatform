using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Infrastructure.Persistence;

namespace CartService.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly CartDbContext _db;

    public UnitOfWork(CartDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(List<OutboxMessage> outboxMessages, CancellationToken ct = default)
    {
        if (outboxMessages.Count > 0)
            _db.OutboxMessages.AddRange(outboxMessages);

        await _db.SaveChangesAsync(ct);
    }
}
