using PaymentService.Domain.SeedWork;

namespace PaymentService.Domain.Aggregates;

public class Payment : AggregateRoot
{ 
    public required Guid OrderId { get; init; }
    public long AmountCents { get; private set; }   
    public string Currency { get; private set; } = String.Empty;  
    public PaymentStatus Status { get; private set; }
    public string ExternalPaymentId { get; private set; } = string.Empty;
    public string PaymentUrl { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Payment() { }

    public static Payment Create(Guid orderId, long amount, string currency)
    {
        if (Guid.Empty == orderId)
            throw new ArgumentException("Order id is required", nameof(orderId));

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AmountCents = amount,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
            Status = PaymentStatus.Pending,
        };
    }
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Complete,
    
    Authorized,
    Captured,
    Failed,
    Cancelled,
    Refunded
}
