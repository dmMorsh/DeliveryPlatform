using InventoryService.Application.Interfaces;

namespace InventoryService.Application.Utils;

public class HashShardResolver : IShardResolver
{
    private readonly int _shardCount;

    public HashShardResolver(int shardCount)
    {
        _shardCount = shardCount;
    }

    public int ResolveShard(Guid productId)
        => Math.Abs(productId.GetHashCode()) % _shardCount;
}
