namespace WebApp.Models;

public class OrderViewModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";

    public Guid ClientId { get; set; }
    public Guid? CourierId { get; set; }

    public string Status { get; set; } = "";
    public string? PaymentStatus { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentUrl { get; set; }
    public Guid? PaymentId { get; set; }

    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Description { get; set; } = "";
    public int WeightGrams { get; set; }

    public long CostCents { get; set; }
    public string? Currency { get; set; }
    public string? CourierNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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

    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderItemViewModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int PriceCents { get; set; }
}
