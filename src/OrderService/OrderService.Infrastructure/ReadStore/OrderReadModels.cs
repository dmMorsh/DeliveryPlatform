namespace OrderService.Infrastructure.ReadStore;

public sealed class OrderReadModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Guid? CourierId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public int Status { get; set; }
    public long CostCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? CourierNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public bool IsReadyForDelivery { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ExpectedReadyAt { get; set; }
    public DateTime? KitchenSlotStart { get; set; }
    public DateTime? KitchenDelayedNotifiedAt { get; set; }
    public string? DeliveryZoneId { get; set; }
    public string? DeliveryZoneName { get; set; }
    public double? DeliveryZoneDistanceKm { get; set; }
    public int? DeliveryPickupSlaMinutes { get; set; }
    public int? DeliveryTransitSlaMinutes { get; set; }
    public double? DeliveryFeeMultiplier { get; set; }
    public bool KitchenSlotCounted { get; set; }

    public List<OrderReadItem> Items { get; set; } = new();
}

public sealed class OrderReadItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PriceCents { get; set; }
    public int Quantity { get; set; }
}

public sealed class KitchenSlot
{
    public DateTime SlotStart { get; set; }
    public int Count { get; set; }
}
