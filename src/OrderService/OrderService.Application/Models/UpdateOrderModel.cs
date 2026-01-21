namespace OrderService.Application;

public class UpdateOrderModel
{
    public Guid? CourierId { get; set; }
    public int? Status { get; set; }
    public string? CourierNote { get; set; }
}