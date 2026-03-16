using CartService.Application.Interfaces;
using Shared.Contracts;
using Shared.Services;

namespace CartService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly CartDbContext _db;

    public UnitOfWork(CartDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(List<OutboxMessage> outboxMessages, CancellationToken ct)
    {
        if (outboxMessages.Count > 0)
            _db.OutboxMessages.AddRange(outboxMessages);

        await _db.SaveChangesWithConcurrencyRetryAsync(maxRetries: 3, ct);
    }
}
