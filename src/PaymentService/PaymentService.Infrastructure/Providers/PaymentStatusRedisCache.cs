using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace PaymentService.Infrastructure.Providers;

public class PaymentStatusRedisCache : IPaymentStatusCache
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public PaymentStatusRedisCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<PaymentStatusView?> GetAsync(Guid orderId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(orderId);
        var value = await db.StringGetAsync(key);

        if (!value.HasValue) return null;

        return JsonSerializer.Deserialize<PaymentStatusView>(value.ToString());
    }

    public async Task SetAsync(Guid orderId, PaymentStatusView view, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(orderId);
        var json = JsonSerializer.Serialize(view);
        await db.StringSetAsync(key, json, CacheDuration);
    }

    public async Task RemoveAsync(Guid orderId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(orderId);
        await db.KeyDeleteAsync(key);
    }

    private static RedisKey CacheKey(Guid orderId) => $"payment_status:{orderId}";
}
