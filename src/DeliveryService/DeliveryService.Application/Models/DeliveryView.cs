using DeliveryService.Domain.Aggregates;

namespace DeliveryService.Application.Models;

public record DeliveryView
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public Guid? CourierId { get; set; }
    public int Status { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? InTransitAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? EstimatedPickupAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public double? EstimatedDistanceKm { get; set; }
    public int? EstimatedTravelMinutes { get; set; }
    public string? DeliveryZoneId { get; set; }
    public string? DeliveryZoneName { get; set; }
    public int? DeliveryPickupSlaMinutes { get; set; }
    public int? DeliveryTransitSlaMinutes { get; set; }
    public double? DeliveryFeeMultiplier { get; set; }
    public double? DeliveryZoneDistanceKm { get; set; }
    public string? Signature { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public string? VerificationCode { get; set; }

    public static DeliveryView From(Delivery delivery)
    {
        return new DeliveryView
        {
            Id = delivery.Id,
            OrderId = delivery.OrderId,
            ClientId = delivery.ClientId,
            CourierId = delivery.CourierId,
            Status = (int)delivery.Status,
            FromAddress = delivery.FromAddress,
            ToAddress = delivery.ToAddress,
            FromLatitude = delivery.FromLatitude,
            FromLongitude = delivery.FromLongitude,
            ToLatitude = delivery.ToLatitude,
            ToLongitude = delivery.ToLongitude,
            CreatedAt = delivery.CreatedAt,
            AssignedAt = delivery.AssignedAt,
            AcceptedAt = delivery.AcceptedAt,
            PickedUpAt = delivery.PickedUpAt,
            InTransitAt = delivery.InTransitAt,
            DeliveredAt = delivery.DeliveredAt,
            CancelledAt = delivery.CancelledAt,
            FailedAt = delivery.FailedAt,
            ReturnedAt = delivery.ReturnedAt,
            EstimatedPickupAt = delivery.EstimatedPickupAt,
            EstimatedDeliveryAt = delivery.EstimatedDeliveryAt,
            EstimatedDistanceKm = delivery.EstimatedDistanceKm,
            EstimatedTravelMinutes = delivery.EstimatedTravelMinutes,
            DeliveryZoneId = delivery.DeliveryZoneId,
            DeliveryZoneName = delivery.DeliveryZoneName,
            DeliveryPickupSlaMinutes = delivery.DeliveryPickupSlaMinutes,
            DeliveryTransitSlaMinutes = delivery.DeliveryTransitSlaMinutes,
            DeliveryFeeMultiplier = delivery.DeliveryFeeMultiplier,
            DeliveryZoneDistanceKm = delivery.DeliveryZoneDistanceKm,
            Signature = delivery.Signature,
            PhotoUrl = delivery.PhotoUrl,
            Notes = delivery.Notes,
            VerificationCode = delivery.VerificationCode
        };
    }
}
