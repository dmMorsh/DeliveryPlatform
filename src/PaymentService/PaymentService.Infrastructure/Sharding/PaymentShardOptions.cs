namespace PaymentService.Infrastructure.Sharding;

public sealed class PaymentShardOptions
{
    public List<PaymentShardInfo> Shards { get; set; } = new();
}

public sealed class PaymentShardInfo
{
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
