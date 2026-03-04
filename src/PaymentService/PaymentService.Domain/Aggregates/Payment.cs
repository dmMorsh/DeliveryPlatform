using PaymentService.Domain.SeedWork;

namespace PaymentService.Domain.Aggregates;

public class Payment : AggregateRoot
{ 
    public required Guid OrderId { get; init; }
    
    public required long AmountCents { get; init; }   
    public required string Currency { get; init; } 
    
    public PaymentStatus Status { get; private set; }
    
    public string Provider { get; private set; } = string.Empty;
    
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
            Status = PaymentStatus.Created,
        };
    }

    public void Start(string provider, string externalPaymentId, string paymentUrl)
    {
        if (Status != PaymentStatus.Created)
            throw new DomainException("Payment already started");

        Status = PaymentStatus.Pending;
        Provider = provider;
        ExternalPaymentId = externalPaymentId;
        PaymentUrl = paymentUrl;
    }

    public void MarkReady()
    {
        if (Status is not (PaymentStatus.Created or PaymentStatus.Starting))
            return;

        Status = PaymentStatus.Ready;
    }

    public void MarkAuthorized(string externalId)
    {
        if (Status != PaymentStatus.Pending)
            return;

        Status = PaymentStatus.Authorized;
        ExternalPaymentId = externalId;
    }

    public void MarkCaptured(string externalId)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            return;

        Status = PaymentStatus.Captured;
        ExternalPaymentId = externalId;
    }

    public void MarkCancelled()
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded)
            return;

        Status = PaymentStatus.Cancelled;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Captured)
            return;

        Status = PaymentStatus.Refunded;
    }

    public void MarkFailed(string reason)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            return;

        Status = PaymentStatus.Failed;
    }
    
}

public enum PaymentStatus
{
    Created = 0,
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6,
    Ready = 7,
    Starting = 8
}
