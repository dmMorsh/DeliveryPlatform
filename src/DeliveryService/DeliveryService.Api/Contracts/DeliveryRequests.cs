namespace DeliveryService.Api.Contracts;

public record AcceptDeliveryRequest
{
    public Guid CourierId { get; init; }
}

public record DeclineDeliveryRequest
{
    public Guid CourierId { get; init; }
    public string? Reason { get; init; }
}

public record CourierOfferDeclineRequest
{
    public string? Reason { get; init; }
}

public record CourierActionRequest
{
    public Guid CourierId { get; init; }
}

public record CompleteDeliveryRequest
{
    public Guid CourierId { get; init; }
    public string? Signature { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Notes { get; init; }
    public string? VerificationCode { get; init; }
}

public record CancelDeliveryRequest
{
    public string? Reason { get; init; }
}

public record FailDeliveryRequest
{
    public string? Reason { get; init; }
}

public record ReturnDeliveryRequest
{
    public string? Reason { get; init; }
}
