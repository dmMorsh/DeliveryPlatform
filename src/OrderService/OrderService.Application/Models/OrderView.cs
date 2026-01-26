namespace OrderService.Application.Models;

public record OrderView
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Guid? CourierId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public int Status { get; set; }
    public decimal CostCents { get; set; }
    public string? CourierNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyCollection<OrderViewItem> Items { get; init; } = Array.Empty<OrderViewItem>();
}

public record OrderViewItem(Guid ProductId,  string Name, int Price, int Quantity);