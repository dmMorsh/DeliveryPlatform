using System.Text.Json;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.Models;
using StackExchange.Redis;

namespace OrderReadService.Infrastructure.Services;

public class OrderReadRedisCache : IOrderReadCache
{
    private readonly IDatabase _db;
    private const string Prefix = "order_read:";

    public OrderReadRedisCache(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<OrderReadModel?> GetAsync(Guid orderId, CancellationToken ct)
    {
        var key = Prefix + orderId;
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue) return null;
        // redis returns RedisValue which can be treated as string
        return JsonSerializer.Deserialize<OrderReadModel>((string)value!);
    }

    public Task SetAsync(OrderReadModel view, CancellationToken ct)
    {
        var key = Prefix + view.Id;
        var data = JsonSerializer.Serialize(view);
        // TTL 30 seconds by default
        return _db.StringSetAsync(key, data, TimeSpan.FromSeconds(30));
    }

    public Task RemoveAsync(Guid orderId, CancellationToken ct)
    {
        var key = Prefix + orderId;
        return _db.KeyDeleteAsync(key);
    }
}
