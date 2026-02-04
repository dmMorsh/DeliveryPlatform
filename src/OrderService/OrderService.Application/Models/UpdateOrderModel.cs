using OrderService.Domain.Aggregates;

namespace OrderService.Application.Models;

public record UpdateOrderModel
{
    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }
    public OrderStatus? Status { get; set; }
    public string? CourierNote { get; set; }
}