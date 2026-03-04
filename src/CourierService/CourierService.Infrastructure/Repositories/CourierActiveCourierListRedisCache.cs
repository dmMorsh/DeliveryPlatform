using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace CourierService.Infrastructure.Repositories;

public class CourierActiveCourierListRedisCache : ICourierActiveCourierListCache
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
    private const string CacheKey = "active_couriers_list";

    public CourierActiveCourierListRedisCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<List<CourierView>?> GetAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CacheKey);

        if (!value.HasValue) return null;

        return JsonSerializer.Deserialize<List<CourierView>>(value.ToString());
    }

    public async Task SetAsync(List<CourierView> views, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(views);
        await db.StringSetAsync(CacheKey, json, CacheDuration);
    }

    public async Task RemoveAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKey);
    }
}
