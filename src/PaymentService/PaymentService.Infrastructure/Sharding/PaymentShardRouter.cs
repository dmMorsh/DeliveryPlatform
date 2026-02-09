using Microsoft.Extensions.Options;

namespace PaymentService.Infrastructure.Sharding;

public sealed class PaymentShardRouter : IPaymentShardRouter
{
    private readonly PaymentShardOptions _options;

    public PaymentShardRouter(IOptions<PaymentShardOptions> options)
    {
        _options = options.Value;
    }

    public string GetConnectionString(Guid orderId)
    {
        var shards = _options.Shards;
        if (shards.Count == 0)
            throw new InvalidOperationException("No payment shards configured");

        var index = GetShardIndex(orderId, shards.Count);
        var connectionString = shards[index].ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Payment shard connection string is empty for index {index}");

        return connectionString;
    }

    public IReadOnlyList<string> GetAllConnectionStrings()
    {
        return _options.Shards
            .Select(s => s.ConnectionString)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    private static int GetShardIndex(Guid key, int shardCount)
    {
        var bytes = key.ToByteArray();
        var value = BitConverter.ToInt64(bytes, 0);
        var hash = value == long.MinValue ? long.MaxValue : Math.Abs(value);
        return (int)(hash % shardCount);
    }
}
