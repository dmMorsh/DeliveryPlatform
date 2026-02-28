using System.Security.Cryptography;
using DeliveryService.Domain.Entities;
using DeliveryService.Domain.Events;
using DeliveryService.Domain.SeedWork;

namespace DeliveryService.Domain.Aggregates;

public enum DeliveryStatus
{
    PendingPayment = 0,
    Assigning = 1,
    Assigned = 2,
    PickedUp = 3,
    InDelivery = 4,
    Delivered = 5,
    Cancelled = 6,
    Failed = 7,
    Returned = 8
}

public enum DeliveryAssignmentStatus
{
    Offered = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3
}

public class Delivery : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public Guid ClientId { get; private set; }

    public string FromAddress { get; private set; } = string.Empty;
    public string ToAddress { get; private set; } = string.Empty;
    public double FromLatitude { get; private set; }
    public double FromLongitude { get; private set; }
    public double ToLatitude { get; private set; }
    public double ToLongitude { get; private set; }

    public DeliveryStatus Status { get; private set; } = DeliveryStatus.PendingPayment;
    public Guid? CourierId { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? InTransitAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public DateTime? PickupTimeoutNotifiedAt { get; private set; }
    public DateTime? TransitTimeoutNotifiedAt { get; private set; }
    public DateTime? EstimatedPickupAt { get; private set; }
    public DateTime? EstimatedDeliveryAt { get; private set; }
    public double? EstimatedDistanceKm { get; private set; }
    public int? EstimatedTravelMinutes { get; private set; }
    public int ReassignAttempts { get; private set; }
    public DateTime? LastReassignAt { get; private set; }

    public string? VerificationCode { get; private set; }
    public DateTime? VerificationGeneratedAt { get; private set; }
    public string? Signature { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Notes { get; private set; }
    public string? DeliveryZoneId { get; private set; }
    public string? DeliveryZoneName { get; private set; }
    public int? DeliveryPickupSlaMinutes { get; private set; }
    public int? DeliveryTransitSlaMinutes { get; private set; }
    public double? DeliveryFeeMultiplier { get; private set; }
    public double? DeliveryZoneDistanceKm { get; private set; }

    public Guid? CurrentOfferCourierId { get; private set; }
    public DateTime? CurrentOfferExpiresAt { get; private set; }

    private readonly List<DeliveryAssignmentAttempt> _assignmentAttempts = new();
    public IReadOnlyCollection<DeliveryAssignmentAttempt> AssignmentAttempts => _assignmentAttempts.AsReadOnly();

    private Delivery() { }

    public static Delivery Create(
        Guid orderId,
        Guid clientId,
        string fromAddress,
        string toAddress,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude)
    {
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ClientId = clientId,
            FromAddress = fromAddress,
            ToAddress = toAddress,
            FromLatitude = fromLatitude,
            FromLongitude = fromLongitude,
            ToLatitude = toLatitude,
            ToLongitude = toLongitude,
            Status = DeliveryStatus.PendingPayment,
            CreatedAt = DateTime.UtcNow
        };

        delivery.AddDomainEvent(new DeliveryCreatedDomainEvent
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId
        });

        return delivery;
    }

    public void StartAssignment()
    {
        if (Status is DeliveryStatus.Cancelled or DeliveryStatus.Failed or DeliveryStatus.Returned or DeliveryStatus.Delivered)
            throw new DomainException("Delivery already finished");

        if (Status == DeliveryStatus.Assigning || Status == DeliveryStatus.Assigned)
            return;

        EnsureTransition(DeliveryStatus.Assigning);
        Status = DeliveryStatus.Assigning;
    }

    public void OfferToCourier(Guid courierId, DateTime expiresAt)
    {
        if (Status != DeliveryStatus.Assigning)
            throw new DomainException("Delivery is not in assigning status");

        CurrentOfferCourierId = courierId;
        CurrentOfferExpiresAt = expiresAt;
        _assignmentAttempts.Add(DeliveryAssignmentAttempt.Offer(courierId, expiresAt));
    }

    public void ExpireCurrentOffer()
    {
        if (!CurrentOfferCourierId.HasValue)
            return;

        var attempt = _assignmentAttempts
            .LastOrDefault(a => a.CourierId == CurrentOfferCourierId && a.Status == DeliveryAssignmentStatus.Offered);
        attempt?.Expire();

        CurrentOfferCourierId = null;
        CurrentOfferExpiresAt = null;
    }

    public void AcceptOffer(Guid courierId)
    {
        if (Status != DeliveryStatus.Assigning)
            throw new DomainException("Delivery is not in assigning status");

        if (CurrentOfferCourierId != courierId)
            throw new DomainException("No active offer for this courier");

        var attempt = _assignmentAttempts
            .LastOrDefault(a => a.CourierId == courierId && a.Status == DeliveryAssignmentStatus.Offered);
        if (attempt == null)
            throw new DomainException("Offer not found");

        attempt.Accept();

        CourierId = courierId;
        AssignedAt = DateTime.UtcNow;
        AcceptedAt = DateTime.UtcNow;
        EnsureTransition(DeliveryStatus.Assigned);
        Status = DeliveryStatus.Assigned;
        CurrentOfferCourierId = null;
        CurrentOfferExpiresAt = null;

        GenerateVerificationCode();

        AddDomainEvent(new DeliveryAssignedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = courierId
        });

        AddDomainEvent(new DeliveryAcceptedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = courierId
        });
    }

    public void DeclineOffer(Guid courierId, string? reason)
    {
        if (Status != DeliveryStatus.Assigning)
            throw new DomainException("Delivery is not in assigning status");

        if (CurrentOfferCourierId != courierId)
            throw new DomainException("No active offer for this courier");

        var attempt = _assignmentAttempts
            .LastOrDefault(a => a.CourierId == courierId && a.Status == DeliveryAssignmentStatus.Offered);
        if (attempt == null)
            throw new DomainException("Offer not found");

        attempt.Decline(reason);
        CurrentOfferCourierId = null;
        CurrentOfferExpiresAt = null;

        AddDomainEvent(new DeliveryDeclinedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = courierId,
            Reason = reason
        });
    }

    public void MarkPickedUp()
    {
        EnsureCourierAssigned();
        if (Status != DeliveryStatus.Assigned)
            throw new DomainException("Delivery is not assigned");

        EnsureTransition(DeliveryStatus.PickedUp);
        Status = DeliveryStatus.PickedUp;
        PickedUpAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryPickedUpDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId!.Value
        });
    }

    public void MarkInTransit()
    {
        EnsureCourierAssigned();
        if (Status != DeliveryStatus.PickedUp)
            throw new DomainException("Delivery is not picked up");

        EnsureTransition(DeliveryStatus.InDelivery);
        Status = DeliveryStatus.InDelivery;
        InTransitAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryInTransitDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId!.Value
        });
    }

    public void Complete(string? signature, string? photoUrl, string? notes, string? verificationCode)
    {
        EnsureCourierAssigned();
        if (Status != DeliveryStatus.InDelivery)
            throw new DomainException("Delivery is not in transit");

        if (!string.IsNullOrWhiteSpace(VerificationCode) && VerificationCode != verificationCode)
            throw new DomainException("Verification code mismatch");

        EnsureTransition(DeliveryStatus.Delivered);
        Status = DeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        Signature = signature;
        PhotoUrl = photoUrl;
        Notes = notes;

        AddDomainEvent(new DeliveryDeliveredDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId!.Value,
            Signature = signature,
            PhotoUrl = photoUrl,
            Notes = notes
        });
    }

    public void Cancel(string? reason)
    {
        if (Status == DeliveryStatus.Delivered)
            throw new DomainException("Delivery already delivered");

        EnsureTransition(DeliveryStatus.Cancelled);
        Status = DeliveryStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryCancelledDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId,
            Reason = reason
        });
    }

    public void Fail(string? reason)
    {
        if (Status == DeliveryStatus.Delivered)
            throw new DomainException("Delivery already delivered");

        EnsureTransition(DeliveryStatus.Failed);
        Status = DeliveryStatus.Failed;
        FailedAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryFailedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId,
            Reason = reason
        });
    }

    public void Return(string? reason)
    {
        if (Status != DeliveryStatus.Failed && Status != DeliveryStatus.Cancelled)
            throw new DomainException("Delivery is not failed or cancelled");

        EnsureTransition(DeliveryStatus.Returned);
        Status = DeliveryStatus.Returned;
        ReturnedAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryReturnedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            CourierId = CourierId,
            Reason = reason
        });
    }

    public void SetEta(DateTime estimatedPickupAt, DateTime estimatedDeliveryAt, double distanceKm, int travelMinutes)
    {
        EstimatedPickupAt = estimatedPickupAt;
        EstimatedDeliveryAt = estimatedDeliveryAt;
        EstimatedDistanceKm = distanceKm;
        EstimatedTravelMinutes = travelMinutes;
    }

    public void SetDeliveryZone(
        string? zoneId,
        string? zoneName,
        int? pickupSlaMinutes,
        int? transitSlaMinutes,
        double? deliveryFeeMultiplier,
        double? zoneDistanceKm)
    {
        DeliveryZoneId = zoneId;
        DeliveryZoneName = zoneName;
        DeliveryPickupSlaMinutes = pickupSlaMinutes;
        DeliveryTransitSlaMinutes = transitSlaMinutes;
        DeliveryFeeMultiplier = deliveryFeeMultiplier;
        DeliveryZoneDistanceKm = zoneDistanceKm;
    }

    public void SetPickupTimeoutNotifiedAt(DateTime value)
    {
        PickupTimeoutNotifiedAt = value;
    }

    public void SetTransitTimeoutNotifiedAt(DateTime value)
    {
        TransitTimeoutNotifiedAt = value;
    }

    public void ResetAssignment(string? reason)
    {
        if (Status != DeliveryStatus.Assigned)
            throw new DomainException("Delivery is not assigned");

        var previousCourierId = CourierId;
        EnsureTransition(DeliveryStatus.Assigning);
        Status = DeliveryStatus.Assigning;
        CourierId = null;
        AssignedAt = null;
        AcceptedAt = null;
        CurrentOfferCourierId = null;
        CurrentOfferExpiresAt = null;

        ReassignAttempts += 1;
        LastReassignAt = DateTime.UtcNow;

        AddDomainEvent(new DeliveryReassignRequestedDomainEvent
        {
            DeliveryId = Id,
            OrderId = OrderId,
            PreviousCourierId = previousCourierId,
            Reason = reason
        });
    }

    public bool CanReassign(int maxAttempts, TimeSpan cooldown, DateTime now)
    {
        if (maxAttempts <= 0)
            return false;
        if (ReassignAttempts >= maxAttempts)
            return false;
        if (cooldown > TimeSpan.Zero && LastReassignAt.HasValue && LastReassignAt.Value.Add(cooldown) > now)
            return false;
        return true;
    }

    private void EnsureTransition(DeliveryStatus to)
    {
        if (Status == to)
            return;

        if (!IsValidTransition(Status, to))
            throw new DomainException($"Invalid delivery status transition {Status} -> {to}");
    }

    private static bool IsValidTransition(DeliveryStatus from, DeliveryStatus to)
    {
        return from switch
        {
            DeliveryStatus.PendingPayment => to is DeliveryStatus.Assigning or DeliveryStatus.Cancelled or DeliveryStatus.Failed,
            DeliveryStatus.Assigning => to is DeliveryStatus.Assigned or DeliveryStatus.Cancelled or DeliveryStatus.Failed,
            DeliveryStatus.Assigned => to is DeliveryStatus.PickedUp or DeliveryStatus.Assigning or DeliveryStatus.Cancelled or DeliveryStatus.Failed,
            DeliveryStatus.PickedUp => to is DeliveryStatus.InDelivery or DeliveryStatus.Failed,
            DeliveryStatus.InDelivery => to is DeliveryStatus.Delivered or DeliveryStatus.Failed,
            DeliveryStatus.Delivered => false,
            DeliveryStatus.Cancelled => to is DeliveryStatus.Returned,
            DeliveryStatus.Failed => to is DeliveryStatus.Returned,
            DeliveryStatus.Returned => false,
            _ => false
        };
    }

    private void EnsureCourierAssigned()
    {
        if (!CourierId.HasValue)
            throw new DomainException("Courier is not assigned");
    }

    private void GenerateVerificationCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        VerificationCode = value.ToString("D6");
        VerificationGeneratedAt = DateTime.UtcNow;
    }
}
