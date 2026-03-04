using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace DeliveryService.Infrastructure.Services;

public class DeliveryOfferRedisCache : IDeliveryOfferCache
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    public DeliveryOfferRedisCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<CourierOfferView?> GetAsync(Guid courierId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(courierId);
        var value = await db.StringGetAsync(key);

        if (!value.HasValue) return null;

        return JsonSerializer.Deserialize<CourierOfferView>(value.ToString());
    }

    public async Task SetAsync(Guid courierId, CourierOfferView view, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(courierId);
        var json = JsonSerializer.Serialize(view);
        await db.StringSetAsync(key, json, CacheDuration);
    }

    public async Task RemoveAsync(Guid courierId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(courierId);
        await db.KeyDeleteAsync(key);
    }

    private static RedisKey CacheKey(Guid courierId) => $"courier_offer:{courierId}";
}
