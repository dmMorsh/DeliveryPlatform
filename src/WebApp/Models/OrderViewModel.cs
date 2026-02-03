namespace WebApp.Models;

public class OrderViewModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";

    public int Status { get; set; }

    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";

    public long CostCents { get; set; }

    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderItemViewModel
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public int PriceCents { get; set; }
}
