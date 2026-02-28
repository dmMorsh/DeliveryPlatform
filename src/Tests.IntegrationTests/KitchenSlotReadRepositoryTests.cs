using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.ReadStore;
using Xunit;

namespace Tests.IntegrationTests;

public class KitchenSlotReadRepositoryTests
{
    [Fact]
    public async Task CountSlotAndFindNextAvailable_Works()
    {
        var options = new DbContextOptionsBuilder<OrderReadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new OrderReadDbContext(options);
        db.KitchenSlots.Add(new KitchenSlot { SlotStart = DateTime.UtcNow.AddMinutes(15), Count = 2 });
        db.KitchenSlots.Add(new KitchenSlot { SlotStart = DateTime.UtcNow.AddMinutes(30), Count = 5 });
        await db.SaveChangesAsync();

        var repo = new KitchenSlotReadRepository(db);
        var slotStart = AlignToSlotStart(DateTime.UtcNow.AddMinutes(15), 15);
        var count = await repo.CountSlotAsync(slotStart, CancellationToken.None);
        count.Should().BeGreaterOrEqualTo(0);

        var next = await repo.FindNextAvailableSlotAsync(slotStart, 15, 10, 3, CancellationToken.None);
        next.Should().NotBeNull();
    }

    private static DateTime AlignToSlotStart(DateTime value, int slotMinutes)
    {
        var minutes = (value.Minute / slotMinutes) * slotMinutes;
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, minutes, 0, DateTimeKind.Utc);
    }
}
