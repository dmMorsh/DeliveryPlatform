namespace OrderService.Api.Contracts;

public record CancelOrderRequest
{
    public string? Reason { get; init; }
}
