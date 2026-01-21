using Microsoft.EntityFrameworkCore;

using CourierService.Infrastructure;

namespace CourierService.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly CourierDbContext _db;

    public UnitOfWork(CourierDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        // Ensure atomic commit of all staged changes (aggregates + outbox messages)
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
