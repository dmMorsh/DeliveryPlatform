using CourierService.Application.Interfaces;
using Shared.Contracts;

namespace CourierService.Infrastructure.Persistence;

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