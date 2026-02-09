namespace Shared.Contracts.Events;

public record PaymentCreatedEvent : IntegrationEvent
{
    public override string EventType => "payment.created";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;

    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required long AmountCents { get; init; }
    public required string Currency { get; init; }
    public required string Provider { get; init; }
    public string? ExternalPaymentId { get; init; }
}

public record PaymentAuthorizedEvent : IntegrationEvent
{
    public override string EventType => "payment.authorized";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;

    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required long AmountCents { get; init; }
    public required string Currency { get; init; }
    public required string Provider { get; init; }
    public required string ExternalPaymentId { get; init; }
}

public record PaymentCapturedEvent : IntegrationEvent
{
    public override string EventType => "payment.captured";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;

    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required long AmountCents { get; init; }
    public required string Currency { get; init; }
    public required string Provider { get; init; }
    public required string ExternalPaymentId { get; init; }
}

public record PaymentFailedEvent : IntegrationEvent
{
    public override string EventType => "payment.failed";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public required Guid PaymentId { get; init; }
    public required string Provider { get; init; }
    public string? ExternalPaymentId { get; init; }
    public required string Reason { get; init; }
}

public record PaymentCancelledEvent : IntegrationEvent
{
    public override string EventType => "payment.cancelled";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public required Guid PaymentId { get; init; }
    public required string Provider { get; init; }
    public string? ExternalPaymentId { get; init; }
}

public record PaymentRefundedEvent : IntegrationEvent
{
    public override string EventType => "payment.refunded";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public required Guid PaymentId { get; init; }
    public required long AmountCents { get; init; }
    public required string Currency { get; init; }
    public required string Provider { get; init; }
    public required string ExternalPaymentId { get; init; }
}
