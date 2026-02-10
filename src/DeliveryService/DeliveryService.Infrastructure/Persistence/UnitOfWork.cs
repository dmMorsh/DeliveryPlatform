using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;

namespace DeliveryService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly DeliveryDbContext _db;

    public UnitOfWork(DeliveryDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(IEnumerable<OutboxMessage> outboxMessages, CancellationToken ct = default)
    {
        var messages = outboxMessages.ToList();
        if (messages.Count > 0)
            _db.OutboxMessages.AddRange(messages);

        await _db.SaveChangesAsync(ct);
    }
}
