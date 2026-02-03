namespace OrderService.Application.Utils;

public static class ShardingHelper
{
    public static int TotalShards;

    public static int ShardForProduct(Guid productId)
    {
        var hash = productId.GetHashCode();
        return Math.Abs(hash % TotalShards);
    }
}