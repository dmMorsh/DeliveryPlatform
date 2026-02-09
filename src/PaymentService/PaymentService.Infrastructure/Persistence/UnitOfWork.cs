using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Infrastructure.Repositories;
using PaymentService.Infrastructure.Sharding;

namespace PaymentService.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _db;

    public IPaymentRepository Payments { get; }

    public UnitOfWork(
        PaymentDbContext db,
        IPaymentShardMapDbContextFactory mapFactory,
        Microsoft.Extensions.Options.IOptions<PaymentShardMapOptions> mapOptions)
    {
        _db = db;
        Payments = new PaymentRepository(_db, mapFactory, mapOptions);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await SaveChangesAsync(new List<OutboxMessage>(), ct);
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
