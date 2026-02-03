using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;

namespace CatalogService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _db;

    public UnitOfWork(CatalogDbContext db)
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