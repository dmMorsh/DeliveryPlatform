using InventoryService.Infrastructure.ReadStore;
using InventoryService.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace Tests.IntegrationTests;

public class InventoryReadProjectionTests
{
    [Fact]
    public async Task StockReservedEvent_CreatesOrUpdatesModel()
    {
        var options = new DbContextOptionsBuilder<InventoryReadDbContext>()
            .UseInMemoryDatabase("inv-read-test1").Options;
        await using var context = new InventoryReadDbContext(options);
        var mux = new RedisMockBuilder().BuildRedisMock();
        var redisCache = new InventoryReadRedisCache(mux);
        var projector = new InventoryReadProjector(context, redisCache);

        var evt = new StockReservedEvent {OrderId = Guid.NewGuid(), Items = [new StockItemSnapshot{ProductId = Guid.NewGuid() , Quantity = 5}]};
        await projector.HandleAsync(evt, CancellationToken.None);

        var repo = new InventoryReadRepository(context, redisCache);
        var view = await repo.GetByProductIdAsync(evt.Items.First().ProductId, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal(5, view.ReservedQuantity);
        Assert.Equal(-5, view.AvailableQuantity);
    }

    [Fact]
    public async Task StockReleasedEvent_AdjustsExistingModel()
    {
        var options = new DbContextOptionsBuilder<InventoryReadDbContext>()
            .UseInMemoryDatabase("inv-read-test2").Options;
        await using var context = new InventoryReadDbContext(options);

        // seed existing record
        var existingId = Guid.NewGuid();
        context.StockItems.Add(new StockItemReadModel
        {
            ProductId = existingId,
            TotalQuantity = 10,
            ReservedQuantity = 5,
            AvailableQuantity = 5
        });
        await context.SaveChangesAsync();
        
        var mux = new RedisMockBuilder().BuildRedisMock();
        var redisCache = new InventoryReadRedisCache(mux);
        
        var projector = new InventoryReadProjector(context, redisCache);
        var evt = new StockReleasedEvent { OrderId = Guid.NewGuid(), Items = [new StockItemSnapshot{ProductId = existingId, Quantity = 2 }]};

        await projector.HandleAsync(evt, CancellationToken.None);

        var repo = new InventoryReadRepository(context, redisCache);
        var view = await repo.GetByProductIdAsync(existingId, CancellationToken.None);

        Assert.NotNull(view);
        Assert.Equal(3, view.ReservedQuantity);
        Assert.Equal(7, view.AvailableQuantity);
    }
}
