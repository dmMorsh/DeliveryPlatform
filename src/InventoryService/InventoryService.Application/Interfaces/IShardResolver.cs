namespace InventoryService.Application.Interfaces;

public interface IShardResolver
{
    int ResolveShard(Guid productId);
}