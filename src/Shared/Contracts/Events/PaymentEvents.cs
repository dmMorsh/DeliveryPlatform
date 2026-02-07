namespace Shared.Contracts.Events;

public record PaymentCapturedEvent : IntegrationEvent
{
    public override string EventType => "payment.captured";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;

    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required long AmountCents { get; init; }
}

public record PaymentFailedEvent : IntegrationEvent
{
    public override string EventType => "payment.failed";
    public override int Version => 1;
    public override string AggregateType => "Payment";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public required string Reason { get; init; }
}