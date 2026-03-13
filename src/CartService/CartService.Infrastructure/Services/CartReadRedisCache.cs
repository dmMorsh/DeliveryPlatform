using CartService.Application.Interfaces;
using CartService.Application.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace CartService.Infrastructure.Services;

public sealed class CartReadRedisCache : ICartReadCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _cacheTtl;

    public CartReadRedisCache(IConnectionMultiplexer redis, IOptions<CartReadCacheOptions> options)
    {
        _redis = redis;
        var ttlSeconds = options?.Value?.TtlSeconds ?? 3600;
        if (ttlSeconds <= 0)
            ttlSeconds = 3600;
        _cacheTtl = TimeSpan.FromSeconds(ttlSeconds);
    }

    public async Task<CartView?> GetAsync(Guid customerId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CacheKey(customerId));
        if (!value.HasValue) return null;
        return JsonSerializer.Deserialize<CartView>(value.ToString());
    }

    public async Task SetAsync(Guid customerId, CartView cart, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(cart);
        await db.StringSetAsync(CacheKey(customerId), json, _cacheTtl);
    }

    public async Task InvalidateAsync(Guid customerId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKey(customerId));
    }

    private static RedisKey CacheKey(Guid customerId) => $"cart:{customerId}";
}
