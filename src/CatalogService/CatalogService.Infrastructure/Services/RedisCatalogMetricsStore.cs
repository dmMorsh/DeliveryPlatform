using CatalogService.Application.Interfaces;
using StackExchange.Redis;

namespace CatalogService.Infrastructure.Services;

public sealed class RedisCatalogMetricsStore : ICatalogMetricsStore
{
    private const string SalesKey = "catalog:product:sales";
    private const string ReservedKey = "catalog:product:reserved";
    private const string LastSoldKey = "catalog:product:last_sold";

    private readonly IDatabase _db;

    public RedisCatalogMetricsStore(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    public async Task IncrementProductSalesAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
            return;

        var field = productId.ToString("N");
        await _db.HashIncrementAsync(SalesKey, field, quantity);
        await _db.HashSetAsync(LastSoldKey, field, DateTime.UtcNow.ToString("O"));
    }

    public async Task IncrementReservedQuantityAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
            return;

        var field = productId.ToString("N");
        await _db.HashIncrementAsync(ReservedKey, field, quantity);
    }
}
