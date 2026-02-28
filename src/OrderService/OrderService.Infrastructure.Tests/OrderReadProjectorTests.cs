using System.Threading;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.ReadStore;
using Shared.Contracts.Events;
using Xunit;

namespace OrderService.Infrastructure.Tests;

public class OrderReadProjectorTests
{
    [Fact]
    public async Task Handle_OrderCreated_WithoutKitchenSlot_PersistsOrder()
    {
        var options = new DbContextOptionsBuilder<OrderReadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new OrderReadDbContext(options);
        var projector = new OrderReadProjector(db);

        var evt = new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            OrderNumber = "T1",
            CreatedAt = DateTime.UtcNow,
            Items = new[] { new IntegrationOrderItemSnapshot { ProductId = Guid.NewGuid(), Name = "X", PriceCents = 100, Quantity = 1 } }
        };

        await projector.HandleAsync(evt, CancellationToken.None);
        await db.SaveChangesAsync();

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == evt.OrderId);
        order.Should().NotBeNull();
        order!.OrderNumber.Should().Be("T1");
        order.KitchenSlotCounted.Should().BeFalse();
    }
}
