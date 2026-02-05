namespace OrderService.Application.Interfaces;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create(Guid orderId);
    IUnitOfWork Create(int shardId);
}