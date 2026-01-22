using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using CourierService.Infrastructure.Persistence;

namespace CourierService.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly CourierDbContext _db;

    public UnitOfWork(CourierDbContext db)
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
