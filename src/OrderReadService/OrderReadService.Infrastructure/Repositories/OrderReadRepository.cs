using Microsoft.EntityFrameworkCore;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.Models;
using OrderReadService.Infrastructure.Persistence;
using Shared.Contracts.Events;

namespace OrderReadService.Infrastructure.Repositories;

public class OrderReadRepository : IOrderReadRepository
{
    private readonly OrderReadDbContext _context;

    public OrderReadRepository(OrderReadDbContext context)
    {
        _context = context;
    }

    public async Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                CourierId = o.CourierId,
                Status = ((OrderStatusCode)o.Status).ToString(),
                //Status = ((OrderService.Domain.Aggregates.OrderStatus)o.Status).ToString(),
                FromAddress = o.FromAddress,
                ToAddress = o.ToAddress,
                FromLatitude = o.FromLatitude,
                FromLongitude = o.FromLongitude,
                ToLatitude = o.ToLatitude,
                ToLongitude = o.ToLongitude,
                Description = o.Description,
                WeightGrams = o.WeightGrams,
                CostCents = o.CostCents,
                Currency = o.Currency,
                CourierNote = o.CourierNote,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                AssignedAt = o.AssignedAt,
                DeliveredAt = o.DeliveredAt,
                ReadyAt = o.ReadyAt,
                IsReadyForDelivery = o.IsReadyForDelivery,
                AcceptedAt = o.AcceptedAt,
                RejectedAt = o.RejectedAt,
                RejectionReason = o.RejectionReason,
                ExpectedReadyAt = o.ExpectedReadyAt,
                KitchenSlotStart = o.KitchenSlotStart,
                KitchenDelayedNotifiedAt = o.KitchenDelayedNotifiedAt,
                DeliveryZoneId = o.DeliveryZoneId,
                DeliveryZoneName = o.DeliveryZoneName,
                DeliveryZoneDistanceKm = o.DeliveryZoneDistanceKm,
                DeliveryPickupSlaMinutes = o.DeliveryPickupSlaMinutes,
                DeliveryTransitSlaMinutes = o.DeliveryTransitSlaMinutes,
                DeliveryFeeMultiplier = o.DeliveryFeeMultiplier,
                Items = o.Items.Select(i => new OrderViewItem(i.ProductId, i.Name, i.PriceCents, i.Quantity)).ToArray()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OrderView>> GetByClientIdAsync(Guid clientId, CancellationToken ct)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o=> new OrderView
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId,
                CourierId = o.CourierId,
                Status = ((OrderStatusCode)o.Status).ToString(),
                //Status = ((OrderService.Domain.Aggregates.OrderStatus)o.Status).ToString(),
                FromAddress = o.FromAddress,
                ToAddress = o.ToAddress,
                FromLatitude = o.FromLatitude,
                FromLongitude = o.FromLongitude,
                ToLatitude = o.ToLatitude,
                ToLongitude = o.ToLongitude,
                Description = o.Description,
                WeightGrams = o.WeightGrams,
                CostCents = o.CostCents,
                Currency = o.Currency,
                CourierNote = o.CourierNote,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                AssignedAt = o.AssignedAt,
                DeliveredAt = o.DeliveredAt,
                ReadyAt = o.ReadyAt,
                IsReadyForDelivery = o.IsReadyForDelivery,
                AcceptedAt = o.AcceptedAt,
                RejectedAt = o.RejectedAt,
                RejectionReason = o.RejectionReason,
                ExpectedReadyAt = o.ExpectedReadyAt,
                KitchenSlotStart = o.KitchenSlotStart,
                KitchenDelayedNotifiedAt = o.KitchenDelayedNotifiedAt,
                DeliveryZoneId = o.DeliveryZoneId,
                DeliveryZoneName = o.DeliveryZoneName,
                DeliveryZoneDistanceKm = o.DeliveryZoneDistanceKm,
                DeliveryPickupSlaMinutes = o.DeliveryPickupSlaMinutes,
                DeliveryTransitSlaMinutes = o.DeliveryTransitSlaMinutes,
                DeliveryFeeMultiplier = o.DeliveryFeeMultiplier,
                Items = o.Items.Select(i => new OrderViewItem(i.ProductId, i.Name, i.PriceCents, i.Quantity)).ToArray()
            })
            .ToListAsync(ct);
    }
}
