using StackExchange.Redis;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Caching;

public sealed class RedisKitchenSlotCache : IKitchenSlotCache
{
    private readonly IConnectionMultiplexer _conn;

    public RedisKitchenSlotCache(IConnectionMultiplexer conn)
    {
        _conn = conn;
    }

    private static string Key(DateTime slotStart) => $"kitchen_slot:{slotStart:O}";

    public async Task<int> GetCountAsync(DateTime slotStart, CancellationToken ct)
    {
        var db = _conn.GetDatabase();
        var val = await db.StringGetAsync(Key(slotStart));
        if (!val.HasValue) return 0;
        return (int)val;
    }

    public async Task<bool> TryReserveAsync(DateTime slotStart, int capacity, TimeSpan ttl, CancellationToken ct)
    {
        var db = _conn.GetDatabase();
        // Lua script: if current + 1 > capacity -> return 0, else INCR and set expiry -> return 1
        const string script = @"local cur = redis.call('GET', KEYS[1]);
            if not cur then cur = 0 else cur = tonumber(cur) end
            if cur + 1 > tonumber(ARGV[1]) then return 0 end
            local new = redis.call('INCR', KEYS[1]);
            redis.call('PEXPIRE', KEYS[1], ARGV[2]);
            return 1";

        var res = (int)await db.ScriptEvaluateAsync(script, new RedisKey[] { Key(slotStart) }, new RedisValue[] { capacity, (long)ttl.TotalMilliseconds });
        return res == 1;
    }

    public async Task ReleaseAsync(DateTime slotStart, CancellationToken ct)
    {
        var db = _conn.GetDatabase();
        const string lua = @"local cur = redis.call('GET', KEYS[1]);
            if not cur then return 0 end
            local n = tonumber(cur) - 1
            if n <= 0 then redis.call('DEL', KEYS[1]); return 0 end
            redis.call('SET', KEYS[1], n)
            return n";

        await db.ScriptEvaluateAsync(lua, new RedisKey[] { Key(slotStart) });
    }
}
