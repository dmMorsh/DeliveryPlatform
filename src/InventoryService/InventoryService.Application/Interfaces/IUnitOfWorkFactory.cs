namespace InventoryService.Application.Interfaces;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create(Guid shardKey);
    IUnitOfWork Create(int shardId);
}