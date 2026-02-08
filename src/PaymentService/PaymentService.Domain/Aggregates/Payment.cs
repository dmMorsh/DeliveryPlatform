using PaymentService.Domain.SeedWork;

namespace PaymentService.Domain.Aggregates;

public class Payment : AggregateRoot
{ 
    public required Guid OrderId { get; init; }
    
    public required long AmountCents { get; init; }   
    public required string Currency { get; init; } 
    
    public PaymentStatus Status { get; private set; }
    
    public string Provider { get; private set; }
    
    public string ExternalPaymentId { get; private set; } = string.Empty;
    
    public string PaymentUrl { get; private set; } = string.Empty;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime PaidAt { get; private set; }

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

    public void Start(string provider)
    {
        if (Status != PaymentStatus.Created)
            throw new DomainException("Payment already started");

        Status = PaymentStatus.Pending;
        Provider = provider;
    }

    public void MarkSucceeded(string externalId)
    {
        if (Status != PaymentStatus.Pending)
            return;

        Status = PaymentStatus.Captured;
        ExternalPaymentId = externalId;
    }

    public void MarkFailed(string reason)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Cancelled)
            return;

        Status = PaymentStatus.Failed;
    }
}

public enum PaymentStatus
{
    Created,
    Pending,
    Paid,
    Complete,
    
    Authorized,
    Captured,
    Failed,
    Cancelled,
    Refunded
}
