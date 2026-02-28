using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Aggregates;

public enum OrderStatus
{
    Pending = Shared.Contracts.Events.OrderStatusCode.Pending,
    Reserved = Shared.Contracts.Events.OrderStatusCode.Reserved,
    Confirmed = Shared.Contracts.Events.OrderStatusCode.Confirmed,
    Assigning = Shared.Contracts.Events.OrderStatusCode.Assigning,
    Assigned = Shared.Contracts.Events.OrderStatusCode.Assigned,
    InDelivery = Shared.Contracts.Events.OrderStatusCode.InDelivery,
    Delivered = Shared.Contracts.Events.OrderStatusCode.Delivered,
    Cancelled = Shared.Contracts.Events.OrderStatusCode.Cancelled,
    Failed = Shared.Contracts.Events.OrderStatusCode.Failed
}

public class Order : AggregateRoot
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid ClientId { get; private set; }
    public Guid? CourierId { get; private set; }
    public Address From { get; private set; }
    public Address To { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int WeightGrams { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money CostCents { get; private set; }
    public string? CourierNote { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadyAt { get; private set; }
    public bool IsReadyForDelivery { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ExpectedReadyAt { get; private set; }
    public DateTime? KitchenSlotStart { get; private set; }
    public DateTime? KitchenDelayedNotifiedAt { get; private set; }
    public string? DeliveryZoneId { get; private set; }
    public string? DeliveryZoneName { get; private set; }
    public double? DeliveryZoneDistanceKm { get; private set; }
    public int? DeliveryPickupSlaMinutes { get; private set; }
    public int? DeliveryTransitSlaMinutes { get; private set; }
    public double? DeliveryFeeMultiplier { get; private set; }

    private List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void AssignCourier(Guid courierId)
    {
        if (CourierId.HasValue) return;
        if (Status is not (OrderStatus.Confirmed or OrderStatus.Assigning or OrderStatus.Assigned))
            return;
        CourierId = courierId;
        AssignedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAssignedDomainEvent { OrderId = Id, CourierId = courierId });
        if (Status is OrderStatus.Confirmed or OrderStatus.Assigning)
            Status = OrderStatus.Assigned;
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        var prev = Status;
        if (prev == newStatus) return;

        if (!IsValidTransition(prev, newStatus))
            return;
        
        Status = newStatus;
        if (newStatus == OrderStatus.Delivered && !DeliveredAt.HasValue)
            DeliveredAt = DateTime.UtcNow;
        AddDomainEvent(new OrderStatusChangedDomainEvent { OrderId = Id, PreviousStatus = prev, NewStatus = newStatus });

        if (newStatus == OrderStatus.Cancelled)
        {
            AddDomainEvent(new OrderCanceledDomainEvent
            {
                OrderId = Id,
                CourierId = CourierId
            });
        }

        if (newStatus is OrderStatus.Cancelled or OrderStatus.Failed)
            RequestStockReleaseIfNeeded();
    }

    public void MarkReadyForDelivery()
    {
        if (IsReadyForDelivery)
            return;

        if (RejectedAt.HasValue)
            throw new DomainException("Rejected order cannot be marked as ready");

        IsReadyForDelivery = true;
        ReadyAt = DateTime.UtcNow;
        AddDomainEvent(new OrderReadyDomainEvent
        {
            OrderId = Id,
            ReadyAt = ReadyAt.Value
        });
    }

    public void AcceptByKitchen()
    {
        if (RejectedAt.HasValue)
            throw new DomainException("Order already rejected");
        if (AcceptedAt.HasValue)
            return;
        AcceptedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAcceptedDomainEvent
        {
            OrderId = Id,
            AcceptedAt = AcceptedAt.Value
        });
    }

    public void ScheduleKitchen(DateTime expectedReadyAt, DateTime slotStart)
    {
        ExpectedReadyAt = expectedReadyAt;
        KitchenSlotStart = slotStart;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkKitchenDelayed(DateTime now)
    {
        KitchenDelayedNotifiedAt = now;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDeliveryZone(
        string zoneId,
        string? zoneName,
        double distanceKm,
        int pickupSlaMinutes,
        int transitSlaMinutes,
        double deliveryFeeMultiplier)
    {
        DeliveryZoneId = zoneId;
        DeliveryZoneName = zoneName;
        DeliveryZoneDistanceKm = distanceKm;
        DeliveryPickupSlaMinutes = pickupSlaMinutes;
        DeliveryTransitSlaMinutes = transitSlaMinutes;
        DeliveryFeeMultiplier = deliveryFeeMultiplier;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCreatedEvent()
    {
        if (DomainEvents.OfType<OrderCreatedDomainEvent>().Any())
            return;

        AddDomainEvent(new OrderCreatedDomainEvent
        {
            OrderId = Id,
            OrderNumber = OrderNumber,
            ClientId = ClientId,
            FromAddress = From.Street,
            ToAddress = To.Street,
            FromLatitude = From.Latitude,
            FromLongitude = From.Longitude,
            ToLatitude = To.Latitude,
            ToLongitude = To.Longitude,
            WeightGrams = WeightGrams,
            CostCents = CostCents.AmountCents,
            Currency = CostCents.Currency,
            CourierNote = CourierNote,
            Description = Description,
            CreatedAt = CreatedAt,
            ExpectedReadyAt = ExpectedReadyAt,
            KitchenSlotStart = KitchenSlotStart,
            Items = Items.Select(i => new DomainOrderItemSnapshot
            {
                ProductId = i.ProductId,
                Name = i.Name,
                PriceCents = i.PriceCents,
                Quantity = i.Quantity
            }).ToList(),
            DeliveryZoneId = DeliveryZoneId,
            DeliveryZoneName = DeliveryZoneName,
            DeliveryZoneDistanceKm = DeliveryZoneDistanceKm,
            DeliveryPickupSlaMinutes = DeliveryPickupSlaMinutes,
            DeliveryTransitSlaMinutes = DeliveryTransitSlaMinutes,
            DeliveryFeeMultiplier = DeliveryFeeMultiplier
        });
    }

    public void RejectByKitchen(string? reason)
    {
        if (RejectedAt.HasValue)
            return;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason;
        AddDomainEvent(new OrderRejectedDomainEvent
        {
            OrderId = Id,
            RejectedAt = RejectedAt.Value,
            Reason = RejectionReason
        });
        Cancel("kitchen_rejected");
    }

    private static bool IsValidTransition(OrderStatus from, OrderStatus to)
    {
        return from switch
        {
            OrderStatus.Pending => to is OrderStatus.Reserved or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.Reserved => to is OrderStatus.Confirmed or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.Confirmed => to is OrderStatus.Assigning or OrderStatus.Assigned or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.Assigning => to is OrderStatus.Assigned or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.Assigned => to is OrderStatus.InDelivery or OrderStatus.Cancelled or OrderStatus.Failed,
            OrderStatus.InDelivery => to is OrderStatus.Delivered or OrderStatus.Failed,
            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            OrderStatus.Failed => false,
            _ => false
        };
    }

    public void AddCourierNote(string note)
    {
        CourierNote = note;
    }

    public static Order Create(
        Guid? orderId,
        Guid clientId,
        string fromAddress,
        string toAddress,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        string? description,
        int weightGrams,
        long costCents,
        string? currency,
        string? courierNote,
        List<OrderItem>? items = null)
    {
        if (items == null || !items.Any())
            throw new DomainException("Order must contain items");
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId cannot be empty");

        var order = new Order
        {
            Id = orderId ?? Guid.NewGuid(),
            OrderNumber = Shared.Utilities.OrderNumberGenerator.GenerateOrderNumber(),
            ClientId = clientId,
            From = new Address(fromAddress,fromLatitude,fromLongitude),
            To =  new Address(toAddress,toLatitude,toLongitude),
            Description = description ?? string.Empty,
            WeightGrams = weightGrams,
            CostCents = new Money(costCents, string.IsNullOrWhiteSpace(currency) ? "USD" : currency),
            CourierNote = courierNote,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            _items = items
        };

        return order;
    }

    public void MarkItemsReserved(IReadOnlyCollection<OrderItem> items)
    {
        foreach (var item in items)
        {
            if (item.Status != OrderItemStatus.Pending)
                continue;

            item.MarkReserved();
        }

        if (Items.All(i => i.Status == OrderItemStatus.Reserved))
            ChangeStatus(OrderStatus.Reserved);
    }

    public void MarkItemsReleasing(IReadOnlyCollection<OrderItem> items)
    {
        foreach (var item in items)
        {
            if (item.Status is OrderItemStatus.Releasing)
                continue;

            item.MarkReleasing();
        }
    }

    public void MarkItemsFailed(OrderItem[] items)
    {
        foreach (var item in items)
        {
            if (item.Status is OrderItemStatus.ReservationFailed)
                continue;

            item.MarkReservationFailed();
        }

        ChangeStatus(OrderStatus.Failed);
    }

    public void MarkAsInconsistent(string error)
    {
        Description += Environment.NewLine + error;
        ChangeStatus(OrderStatus.Failed);
        AddDomainEvent(new OrderCriticalErrorDomainEvent
        {
            OrderId = Id, 
            ClientId = ClientId, 
            Description = Description
        });
    }

    public void Cancel(string? reason = null)
    {
        if (Status == OrderStatus.Delivered)
            throw new DomainException("Delivered order cannot be canceled");

        if (Status == OrderStatus.Cancelled)
            return;

        if (!string.IsNullOrWhiteSpace(reason))
            Description += Environment.NewLine + reason;

        ChangeStatus(OrderStatus.Cancelled);
    }

    private void RequestStockReleaseIfNeeded()
    {
        var reservedItems = Items.Where(i => i.Status is OrderItemStatus.Reserved).ToList();
        if (reservedItems.Count == 0)
            return;

        AddDomainEvent(new OrderItemsReleaseDomainEvent
        {
            OrderId = Id,
            Items = reservedItems
                .Select(i => new DomainOrderItemSnapshot
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                }).ToArray()
        });

        reservedItems.ForEach(i => i.MarkReleasing());
    }
}
