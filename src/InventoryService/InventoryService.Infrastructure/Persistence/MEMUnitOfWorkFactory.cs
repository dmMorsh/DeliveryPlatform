using InventoryService.Application.Interfaces;

namespace InventoryService.Infrastructure.Persistence;

public class MemUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly InventoryDbContext _db;

    public MemUnitOfWorkFactory(InventoryDbContext db) => _db = db;

    public IUnitOfWork Create(Guid productId) => new UnitOfWork(_db);

    public IUnitOfWork Create(int shardId) => new UnitOfWork(_db);
}