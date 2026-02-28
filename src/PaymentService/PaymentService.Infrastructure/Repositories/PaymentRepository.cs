using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Sharding;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;
    private readonly IPaymentShardMapDbContextFactory _mapFactory;
    private readonly PaymentShardMapOptions _mapOptions;

    public PaymentRepository(
        PaymentDbContext db,
        IPaymentShardMapDbContextFactory mapFactory,
        IOptions<PaymentShardMapOptions> mapOptions)
    {
        _db = db;
        _mapFactory = mapFactory;
        _mapOptions = mapOptions.Value;
    }

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        await _db.Payments.AddAsync(payment, ct);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Payments.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Payment?> GetByOrderId(Guid orderId, CancellationToken ct = default)
    {
        return await _db.Payments.FirstOrDefaultAsync(x => x.OrderId == orderId, ct);
    }

    public async Task<bool> TryMarkStartingAsync(Guid orderId, CancellationToken ct = default)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE payment.\"Payments\" SET \"Status\" = {(int)PaymentStatus.Starting} WHERE \"OrderId\" = {orderId} AND \"Status\" = {(int)PaymentStatus.Ready}",
            ct);
        return affected > 0;
    }

    public async Task UpsertExternalPaymentIdMap(
        Guid orderId,
        Guid paymentId,
        string externalPaymentId,
        string provider,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalPaymentId))
            return;

        await using var db = CreateShardMapContext();
        var map = await db.PaymentShardMaps.FirstOrDefaultAsync(x => x.ExternalPaymentId == externalPaymentId, ct);
        if (map is null)
        {
            map = new PaymentShardMap
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PaymentId = paymentId,
                ExternalPaymentId = externalPaymentId,
                Provider = provider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await db.PaymentShardMaps.AddAsync(map, ct);
        }
        else
        {
            if (map.OrderId != orderId)
                map.OrderId = orderId;
            if (map.PaymentId != paymentId)
                map.PaymentId = paymentId;
            if (!string.Equals(map.Provider, provider, StringComparison.Ordinal))
                map.Provider = provider;
            map.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private PaymentShardMapDbContext CreateShardMapContext()
    {
        var connectionString = _mapOptions.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Shard map connection string is not configured");

        return _mapFactory.Create(connectionString);
    }

}
