namespace OrderService.Application;

public class CreateOrderModel
{
    public Guid ClientId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public long CostCents { get; set; }
    public string? CourierNote { get; set; }
}