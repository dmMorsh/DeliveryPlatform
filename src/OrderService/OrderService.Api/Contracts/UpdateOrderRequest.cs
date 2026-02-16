using OrderService.Domain.Aggregates;

namespace OrderService.Api.Contracts;

public record UpdateOrderRequest
{
    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }
    public OrderStatus? Status { get; set; }
    public string? CourierNote { get; set; }
}