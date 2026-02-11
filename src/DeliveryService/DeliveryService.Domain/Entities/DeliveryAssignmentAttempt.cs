using DeliveryService.Domain.Aggregates;
using DeliveryService.Domain.SeedWork;

namespace DeliveryService.Domain.Entities;

public class DeliveryAssignmentAttempt : Entity
{
    public Guid CourierId { get; private set; }
    public DeliveryAssignmentStatus Status { get; private set; }
    public DateTime OfferedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? Reason { get; private set; }

    private DeliveryAssignmentAttempt() { }

    public static DeliveryAssignmentAttempt Offer(Guid courierId, DateTime expiresAt)
    {
        return new DeliveryAssignmentAttempt
        {
            CourierId = courierId,
            Status = DeliveryAssignmentStatus.Offered,
            OfferedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Accept()
    {
        Status = DeliveryAssignmentStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Decline(string? reason)
    {
        Status = DeliveryAssignmentStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        Reason = reason;
    }

    public void Expire()
    {
        Status = DeliveryAssignmentStatus.Expired;
        RespondedAt = DateTime.UtcNow;
    }
}