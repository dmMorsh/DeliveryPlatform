using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Persistence;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IShardResolver _resolver;
    private readonly IConfiguration _cfg;

    public UnitOfWorkFactory(IShardResolver resolver, IConfiguration cfg)
    {
        _resolver = resolver;
        _cfg = cfg;
    }

    public IUnitOfWork Create(Guid orderId)
    {
        var shard = _resolver.ResolveShard(orderId);
        var cs = _cfg.GetConnectionString($"OrderShard{shard}");
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(cs)
            .Options;

        var db = new OrderDbContext(options);
        return new UnitOfWork(db);
    }
    
    public IUnitOfWork Create(int shardId)
    {
        var cs = _cfg.GetConnectionString($"OrderShard{shardId}");
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(cs)
            .Options;

        var db = new OrderDbContext(options);
        return new UnitOfWork(db);
    }
}