using DeliveryService.Application.Interfaces;
using StackExchange.Redis;

namespace DeliveryService.Infrastructure.Services;

public class RedisAssignmentQueue : IAssignmentQueue
{
    private const string QueueKey = "delivery:assigning:queue";
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
        var values = await _db.SortedSetRangeByScoreAsync(
            QueueKey,
            double.NegativeInfinity,
            maxScore,
            Exclude.None,
            Order.Ascending,
            0,
            1);

        if (values.Length == 0)
            return null;

        var value = values[0];
        if (value.IsNullOrEmpty)
            return null;

        await _db.SortedSetRemoveAsync(QueueKey, value);

        return Guid.TryParse(value.ToString(), out var id) ? id : null;
    }
}
