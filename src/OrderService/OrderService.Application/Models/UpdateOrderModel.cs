namespace OrderService.Application.Models;

public record UpdateOrderModel
{
    public Guid? CourierId { get; set; }
    public string? CourierName { get; set; }
    public int? Status { get; set; }
    public string? CourierNote { get; set; }
}