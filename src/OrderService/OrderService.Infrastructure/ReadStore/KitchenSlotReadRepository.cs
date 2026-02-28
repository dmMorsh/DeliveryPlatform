using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.ReadStore;

public sealed class KitchenSlotReadRepository : IKitchenSlotReadRepository
{
    private readonly OrderReadDbContext _db;

    public KitchenSlotReadRepository(OrderReadDbContext db)
    {
        _db = db;
    }

    public async Task<int> CountSlotAsync(DateTime slotStart, CancellationToken ct)
    {
        var slot = await _db.KitchenSlots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SlotStart == slotStart, ct);
        return slot?.Count ?? 0;
    }

    public async Task<DateTime?> FindNextAvailableSlotAsync(DateTime slotStart, int slotMinutes, int capacity, int lookaheadSlots, CancellationToken ct)
    {
        if (slotMinutes <= 0 || capacity <= 0 || lookaheadSlots <= 0)
            return null;

        var slots = new List<DateTime>(lookaheadSlots);
        for (var i = 1; i <= lookaheadSlots; i++)
            slots.Add(slotStart.AddMinutes(slotMinutes * i));

        var counts = await _db.KitchenSlots.AsNoTracking()
            .Where(s => slots.Contains(s.SlotStart))
            .ToListAsync(ct);

        foreach (var next in slots)
        {
            var count = counts.FirstOrDefault(s => s.SlotStart == next)?.Count ?? 0;
            if (count < capacity)
                return next;
        }

        return null;
    }
}
