using InventoryService.Application.Models;
using InventoryService.Application.Read;
using StackExchange.Redis;
using System.Text.Json;

namespace InventoryService.Infrastructure.ReadStore;

public class InventoryReadRedisCache : IInventoryReadCache
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public InventoryReadRedisCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<StockItemView?> GetAsync(Guid productId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(productId);
        var value = await db.StringGetAsync(key);
        
        if (!value.HasValue) return null;
        
        return JsonSerializer.Deserialize<StockItemView>(value.ToString());
    }

    public async Task SetAsync(Guid productId, StockItemView view, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(productId);
        var json = JsonSerializer.Serialize(view);
        await db.StringSetAsync(key, json, CacheDuration);
    }

    public async Task RemoveAsync(Guid productId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = CacheKey(productId);
        await db.KeyDeleteAsync(key);
    }

    private static RedisKey CacheKey(Guid productId) => $"stock:{productId}";
}
