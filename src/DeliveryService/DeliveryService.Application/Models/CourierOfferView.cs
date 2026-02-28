namespace DeliveryService.Application.Models;

public sealed record CourierOfferView
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public double FromLatitude { get; init; }
    public double FromLongitude { get; init; }
    public double ToLatitude { get; init; }
    public double ToLongitude { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? EstimatedPickupAt { get; init; }
    public DateTime? EstimatedDeliveryAt { get; init; }
    public double? EstimatedDistanceKm { get; init; }
    public int? EstimatedTravelMinutes { get; init; }
}
