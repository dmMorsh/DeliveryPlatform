namespace OrderService.Application.Interfaces;

public interface IShardResolver
{
    int ResolveShard(Guid productId);
}