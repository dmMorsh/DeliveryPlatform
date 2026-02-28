using DeliveryService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace DeliveryService.Infrastructure.Services;

public sealed class CourierActivityRedisStore : ICourierActivityStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _ttl;

    public CourierActivityRedisStore(IConnectionMultiplexer redis, IConfiguration config)
    {
        _redis = redis;
        var ttlSeconds = int.TryParse(config["Delivery:Courier:HeartbeatTtlSeconds"], out var value) ? value : 45;
        _ttl = ttlSeconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(ttlSeconds);
    }

    public async Task TouchAsync(Guid courierId, DateTime now, CancellationToken ct = default)
    {
        if (_ttl == TimeSpan.Zero)
            return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(GetKey(courierId), now.ToString("O"), _ttl);
    }

    public async Task<bool> IsActiveAsync(Guid courierId, DateTime now, CancellationToken ct = default)
    {
        if (_ttl == TimeSpan.Zero)
            return true;

        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(GetKey(courierId));
    }

    private static string GetKey(Guid courierId) => $"courier:active:{courierId}";
}
