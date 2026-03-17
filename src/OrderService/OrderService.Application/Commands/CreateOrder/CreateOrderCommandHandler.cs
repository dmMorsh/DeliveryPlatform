using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using OrderService.Application.Services;
using Shared.Contracts;

namespace OrderService.Application.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderView>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly KitchenCapacityOptions _kitchenOptions;
    private readonly IDeliveryZoneMatcher _zoneMatcher;
    private readonly DeliveryZoneOptions _zoneOptions;
    private readonly IKitchenSlotRepository _kitchenSlots;
    private readonly IKitchenSlotCache _kitchenCache;

    public CreateOrderCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper, 
        ILogger<CreateOrderCommandHandler> logger,
        IOptions<KitchenCapacityOptions> kitchenOptions,
        IDeliveryZoneMatcher zoneMatcher,
        IOptions<DeliveryZoneOptions> zoneOptions,
        IKitchenSlotRepository kitchenSlots,
        IKitchenSlotCache kitchenCache
        )
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
        _kitchenOptions = kitchenOptions.Value ?? new KitchenCapacityOptions();
        _zoneMatcher = zoneMatcher;
        _zoneOptions = zoneOptions.Value ?? new DeliveryZoneOptions();
        _kitchenSlots = kitchenSlots;
        _kitchenCache = kitchenCache;
    }

    public async Task<ApiResponse<OrderView>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FromAddress) || string.IsNullOrWhiteSpace(request.ToAddress))
            return ApiResponse<OrderView>.ErrorResponse("From and To addresses are required");

        if (request.CostCents <= 0)
            return ApiResponse<OrderView>.ErrorResponse("Cost must be greater than 0");
        
        var order = Mapping.OrderFactory.CreateNew(request);

        await using var uow = _factory.Create(order.Id);

        if (_zoneOptions.Enabled && _zoneOptions.Zones.Count > 0)
        {
            var lat = _zoneOptions.UseToCoordinates ? request.ToLatitude : request.FromLatitude;
            var lon = _zoneOptions.UseToCoordinates ? request.ToLongitude : request.FromLongitude;
            var match = _zoneMatcher.Match(lat, lon);
            if (match == null)
                return ApiResponse<OrderView>.ErrorResponse(ErrorCodes.Conflict, "Address is outside delivery zone.");

            var zone = match.Zone;
            order.SetDeliveryZone(
                zone.Id,
                zone.Name,
                match.DistanceKm,
                zone.PickupSlaMinutes,
                zone.TransitSlaMinutes,
                zone.DeliveryFeeMultiplier);
        }

        if (_kitchenOptions.Enabled)
        {
            if (KitchenPauseState.TryGetPausedUntil(out var pausedUntil))
            {
                var until = pausedUntil!.Value;
                var pauseMessage = $"Kitchen is paused until {until:O}.";
                return ApiResponse<OrderView>.ErrorResponse(ErrorCodes.Conflict, pauseMessage);
            }

            var now = DateTime.UtcNow;
            var slotMinutes = Math.Max(_kitchenOptions.SlotMinutes, 1);
            var capacity = Math.Max(_kitchenOptions.MaxOrdersPerSlot, 1);
            var prepMinutes = Math.Max(_kitchenOptions.PreparationMinutes, 0);
            var lookaheadSlots = Math.Max(_kitchenOptions.LookaheadSlots, 1);

            var expectedReadyAt = request.DesiredReadyAt.HasValue && request.DesiredReadyAt.Value > now
                ? request.DesiredReadyAt.Value
                : now.AddMinutes(prepMinutes);
            var slotStart = AlignToSlotStart(expectedReadyAt, slotMinutes);

            // Try quick reservation via short-term cache first to avoid over-admits
            var reserved = false;
            var cacheTtl = TimeSpan.FromMinutes(Math.Max(slotMinutes, 1) + 5);
            try
            {
                reserved = await _kitchenCache.TryReserveAsync(slotStart, capacity, cacheTtl, ct);
            }
            catch
            {
                reserved = false;
            }

            if (!reserved)
            {
                var slotCount = await _kitchenSlots.CountSlotAsync(slotStart, ct);
                if (slotCount >= capacity)
                {
                    var nextSlot = await _kitchenSlots.FindNextAvailableSlotAsync(
                        slotStart,
                        slotMinutes,
                        capacity,
                        lookaheadSlots,
                        ct);

                    if (!nextSlot.HasValue)
                    {
                        if (_kitchenOptions.PauseMinutesOnOverload > 0)
                            KitchenPauseState.PauseUntil(now.AddMinutes(_kitchenOptions.PauseMinutesOnOverload));

                        return ApiResponse<OrderView>.ErrorResponse(
                            ErrorCodes.Conflict,
                            "Kitchen capacity reached. Please try again later.");
                    }

                    expectedReadyAt = nextSlot.Value;
                    slotStart = nextSlot.Value;
                    // attempt to reserve the next slot in cache (best-effort)
                    try { await _kitchenCache.TryReserveAsync(slotStart, capacity, cacheTtl, ct); } catch { }
                }
            }

            order.ScheduleKitchen(expectedReadyAt, slotStart);
        }

        order.AddCreatedEvent();
        
        await uow.Orders.CreateOrderAsync(order, ct);
        
        var outboxMessages = order.DomainEvents
            .Select(de => _eventMapper.MapFromDomainEvent(de))
            .Where(ie => ie != null)
            .Select(ie => OutboxMessage.From(ie!))
            .ToList();

        try
        {
            await uow.SaveChangesAsync(outboxMessages, ct);
        }
        catch (DbUpdateException) when (request.CheckoutId.HasValue)
        {
            var existing = await uow.Orders.GetOrderByIdAsync(request.CheckoutId.Value, ct);
            if (existing != null)
                return ApiResponse<OrderView>.SuccessResponse(existing.Adapt<OrderView>(), "Order created successfully");
            throw;
        }
        order.ClearDomainEvents();

        _logger.LogInformation("Order created: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);

        var view = order.Adapt<OrderView>();
        return ApiResponse<OrderView>.SuccessResponse(view, "Order created successfully");
    }

    private static DateTime AlignToSlotStart(DateTime value, int slotMinutes)
    {
        if (slotMinutes <= 0)
            return value;

        var minutes = (value.Minute / slotMinutes) * slotMinutes;
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, minutes, 0, DateTimeKind.Utc);
    }
}
