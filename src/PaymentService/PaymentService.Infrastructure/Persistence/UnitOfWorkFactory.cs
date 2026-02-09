using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Sharding;

namespace PaymentService.Infrastructure.Persistence;

public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IPaymentShardRouter _router;
    private readonly IPaymentDbContextFactory _dbFactory;
    private readonly IPaymentShardMapDbContextFactory _mapFactory;
    private readonly PaymentShardMapOptions _mapOptions;

    public UnitOfWorkFactory(
        IPaymentShardRouter router,
        IPaymentDbContextFactory dbFactory,
        IPaymentShardMapDbContextFactory mapFactory,
        IOptions<PaymentShardMapOptions> mapOptions)
    {
        _router = router;
        _dbFactory = dbFactory;
        _mapFactory = mapFactory;
        _mapOptions = mapOptions.Value;
    }

    public IUnitOfWork Create(Guid orderId)
    {
        var connectionString = _router.GetConnectionString(orderId);
        var db = _dbFactory.Create(connectionString);
        return new UnitOfWork(db, _mapFactory, Options.Create(_mapOptions));
    }

    public async Task<Guid?> ResolveOrderIdByExternalPaymentId(string externalPaymentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalPaymentId))
            return null;

        await using var mapDb = CreateShardMapContext();
        var map = await mapDb.PaymentShardMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalPaymentId == externalPaymentId, ct);

        return map?.OrderId;
    }

    private PaymentShardMapDbContext CreateShardMapContext()
    {
        var connectionString = string.IsNullOrWhiteSpace(_mapOptions.ConnectionString)
            ? _router.GetAllConnectionStrings().FirstOrDefault()
            : _mapOptions.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Shard map connection string is not configured");

        return _mapFactory.Create(connectionString);
    }
}
