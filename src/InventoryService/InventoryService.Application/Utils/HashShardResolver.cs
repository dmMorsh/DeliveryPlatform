using InventoryService.Application.Interfaces;

namespace InventoryService.Application.Utils;

public class HashShardResolver : IShardResolver
{
    private readonly int _shardCount;

    public HashShardResolver(int shardCount)
    {
        _shardCount = shardCount;
    }

    public int ResolveShard(Guid id)
    {
        var bytes = id.ToByteArray();
        var hash = BitConverter.ToInt32(bytes, 0);
        return Math.Abs(hash % _shardCount);
    }
}
