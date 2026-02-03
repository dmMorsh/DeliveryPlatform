using InventoryService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InventoryService.Infrastructure.Persistence;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IShardResolver _resolver;
    private readonly IConfiguration _cfg;

    public UnitOfWorkFactory(IShardResolver resolver, IConfiguration cfg)
    {
        _resolver = resolver;
        _cfg = cfg;
    }

    public IUnitOfWork Create(Guid productId)
    {
        var shard = _resolver.ResolveShard(productId);
        var cs = _cfg.GetConnectionString($"InventoryShard{shard}");
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(cs)
            .Options;

        var db = new InventoryDbContext(options);
        return new UnitOfWork(db);
    }
    
    public IUnitOfWork Create(int shardId)
    {
        var cs = _cfg.GetConnectionString($"InventoryShard{shardId}");
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(cs)
            .Options;

        var db = new InventoryDbContext(options);
        return new UnitOfWork(db);
    }
}