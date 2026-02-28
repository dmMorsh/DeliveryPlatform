using DeliveryService.Application.Interfaces;
using StackExchange.Redis;

namespace DeliveryService.Infrastructure.Services;

public class RedisAssignmentQueue : IAssignmentQueue
{
    private const string QueueKey = "delivery:assigning:queue";
    private const string DequeueScript = """
        local vals = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, 1)
        if #vals == 0 then
            return {}
        end
        redis.call('ZREM', KEYS[1], vals[1])
        return vals
        """;
    private readonly IDatabase _db;

    public RedisAssignmentQueue(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    public async Task EnqueueAsync(Guid deliveryId, DateTimeOffset availableAt, bool onlyIfMissing = false, CancellationToken ct = default)
    {
        var score = availableAt.ToUnixTimeMilliseconds();
        var when = onlyIfMissing ? When.NotExists : When.Always;
        await _db.SortedSetAddAsync(QueueKey, deliveryId.ToString(), score, when);
    }

    public async Task<Guid?> DequeueReadyAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var maxScore = now.ToUnixTimeMilliseconds();
        var result = await _db.ScriptEvaluateAsync(
            DequeueScript,
            new RedisKey[] { QueueKey },
            new RedisValue[] { maxScore });

        if (result.IsNull)
            return null;

        RedisResult[]? values;
        try
        {
            values = (RedisResult[]?)result;
        }
        catch (InvalidCastException)
        {
            return null;
        }

        if (values == null || values.Length == 0)
            return null;

        var value = values[0].ToString();
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
