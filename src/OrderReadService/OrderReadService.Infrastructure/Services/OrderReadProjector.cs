using Microsoft.EntityFrameworkCore;
using OrderReadService.Application.Interfaces;
using OrderReadService.Domain.Models;
using OrderReadService.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderReadService.Infrastructure.Services;

public sealed class OrderReadProjector(OrderReadDbContext db) : IOrderReadProjector
{
    public async Task HandleAsync(OrderCreatedEvent evt, CancellationToken ct)
    {
        var existing = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);

        var now = DateTime.UtcNow;
        if (existing == null)
        {
            existing = new OrderReadModel
            {
                Id = evt.OrderId,
                OrderNumber = evt.OrderNumber ?? string.Empty,
                ClientId = evt.ClientId,
                FromAddress = evt.FromAddress,
                ToAddress = evt.ToAddress,
                FromLatitude = evt.FromLatitude,
                FromLongitude = evt.FromLongitude,
                ToLatitude = evt.ToLatitude,
                ToLongitude = evt.ToLongitude,
                Description = evt.Description ?? string.Empty,
                WeightGrams = evt.WeightGrams,
                Status = (int)OrderStatusCode.Pending,
                CostCents = evt.CostCents,
                Currency = evt.Currency,
                CourierNote = evt.CourierNote,
                CreatedAt = evt.CreatedAt == default ? now : evt.CreatedAt,
                UpdatedAt = now,
                ExpectedReadyAt = evt.ExpectedReadyAt,
                KitchenSlotStart = evt.KitchenSlotStart,
                DeliveryZoneId = evt.DeliveryZoneId,
                DeliveryZoneName = evt.DeliveryZoneName,
                DeliveryZoneDistanceKm = evt.DeliveryZoneDistanceKm,
                DeliveryPickupSlaMinutes = evt.DeliveryPickupSlaMinutes,
                DeliveryTransitSlaMinutes = evt.DeliveryTransitSlaMinutes,
                DeliveryFeeMultiplier = evt.DeliveryFeeMultiplier,
                KitchenSlotCounted = false
            };
            db.Orders.Add(existing);
        }
        else
        {
            existing.OrderNumber = evt.OrderNumber ?? existing.OrderNumber;
            existing.ClientId = evt.ClientId;
            existing.FromAddress = evt.FromAddress;
            existing.ToAddress = evt.ToAddress;
            existing.FromLatitude = evt.FromLatitude;
            existing.FromLongitude = evt.FromLongitude;
            existing.ToLatitude = evt.ToLatitude;
            existing.ToLongitude = evt.ToLongitude;
            existing.Description = evt.Description ?? existing.Description;
            existing.WeightGrams = evt.WeightGrams;
            existing.CostCents = evt.CostCents;
            existing.Currency = evt.Currency;
            existing.CourierNote = evt.CourierNote ?? existing.CourierNote;
            existing.ExpectedReadyAt = evt.ExpectedReadyAt ?? existing.ExpectedReadyAt;
            existing.KitchenSlotStart = evt.KitchenSlotStart ?? existing.KitchenSlotStart;
            existing.DeliveryZoneId = evt.DeliveryZoneId;
            existing.DeliveryZoneName = evt.DeliveryZoneName;
            existing.DeliveryZoneDistanceKm = evt.DeliveryZoneDistanceKm;
            existing.DeliveryPickupSlaMinutes = evt.DeliveryPickupSlaMinutes;
            existing.DeliveryTransitSlaMinutes = evt.DeliveryTransitSlaMinutes;
            existing.DeliveryFeeMultiplier = evt.DeliveryFeeMultiplier;
            existing.UpdatedAt = now;
        }

        if (evt.Items.Count > 0)
        {
            existing.Items.Clear();
            foreach (var item in evt.Items)
            {
                existing.Items.Add(new OrderReadItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = evt.OrderId,
                    ProductId = item.ProductId,
                    Name = item.Name,
                    PriceCents = (int)item.PriceCents,
                    Quantity = item.Quantity
                });
            }
        }

        if (!existing.KitchenSlotCounted && existing.KitchenSlotStart.HasValue)
        {
            await IncrementKitchenSlotAsync(existing.KitchenSlotStart.Value, ct);
            existing.KitchenSlotCounted = true;
        }
    }

    public async Task HandleAsync(OrderStatusChangedEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.Status = evt.NewStatus;
        order.UpdatedAt = evt.ChangedAt == default ? DateTime.UtcNow : evt.ChangedAt;

        if (evt.NewStatus == (int)OrderStatusCode.Cancelled || evt.NewStatus == (int)OrderStatusCode.Failed)
            await TryDecrementKitchenSlotAsync(order, ct);
    }

    public async Task HandleAsync(OrderReadyEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.IsReadyForDelivery = true;
        order.ReadyAt = evt.ReadyAt;
        order.UpdatedAt = DateTime.UtcNow;
    }

    public async Task HandleAsync(OrderAcceptedEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.AcceptedAt = evt.AcceptedAt;
        order.UpdatedAt = DateTime.UtcNow;
    }

    public async Task HandleAsync(OrderRejectedEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.RejectedAt = evt.RejectedAt;
        order.RejectionReason = evt.Reason;
        order.UpdatedAt = DateTime.UtcNow;
        await TryDecrementKitchenSlotAsync(order, ct);
    }

    public async Task HandleAsync(OrderCanceledEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.Status = (int)OrderStatusCode.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await TryDecrementKitchenSlotAsync(order, ct);
    }

    public async Task HandleAsync(OrderKitchenDelayedEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.KitchenDelayedNotifiedAt = evt.DetectedAt;
        order.UpdatedAt = DateTime.UtcNow;
    }

    public async Task HandleAsync(OrderAssignedEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.CourierId = evt.CourierId;
        order.CourierName = evt.CourierName;
        order.CourierPhone = evt.CourierPhone;
        order.AssignedAt = evt.Timestamp == default ? DateTime.UtcNow : evt.Timestamp;
        order.EstimatedDeliveryAt = evt.EstimatedDeliveryAt;
        if (evt.EstimatedPickupAt.HasValue && order.ReadyAt.HasValue)
        {
            order.EstimatedArrivalMinutes = (int?)Math.Max(0,
                (evt.EstimatedPickupAt.Value - order.ReadyAt.Value).TotalMinutes);
        }
        order.UpdatedAt = DateTime.UtcNow;
    }

    public async Task HandleAsync(OrderDeliveredEvent evt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
        if (order == null)
            return;

        order.DeliveredAt = evt.DeliveredAt;
        order.Status = (int)OrderStatusCode.Delivered;
        order.UpdatedAt = DateTime.UtcNow;
    }

    private async Task IncrementKitchenSlotAsync(DateTime slotStart, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "order_read"."KitchenSlots" ("SlotStart", "Count")
            VALUES ({slotStart}, 1)
            ON CONFLICT ("SlotStart")
            DO UPDATE SET "Count" = "KitchenSlots"."Count" + 1
        """, ct);
    }

    private async Task TryDecrementKitchenSlotAsync(OrderReadModel order, CancellationToken ct)
    {
        if (order.Status == (int)OrderStatusCode.Cancelled || order.Status == (int)OrderStatusCode.Failed)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE "order_read"."KitchenSlots"
                SET "Count" = GREATEST("Count" - 1, 0)
                WHERE "SlotStart" = {order.KitchenSlotStart}
            """, ct);
        }
    }
    
    private async Task _TryDecrementKitchenSlotAsync(OrderReadModel order, CancellationToken ct)
    {
        if (!order.KitchenSlotCounted || order.KitchenSlotStart == null)
            return;
        if (order.IsReadyForDelivery)
            return;
        var slotStart = order.KitchenSlotStart.Value;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "order_read"."KitchenSlots" ("SlotStart", "Count")
            VALUES ({slotStart}, 0)
            ON CONFLICT ("SlotStart")
            DO UPDATE SET "Count" = GREATEST("KitchenSlots"."Count" - 1, 0)
        """, ct);

        order.KitchenSlotCounted = false;
    }
}
