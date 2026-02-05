namespace WebApp.Models;

public class StockItemViewModel
{
    public Guid ProductId { get; set; }

    public int TotalQuantity { get; set; }
    
    public int ReservedQuantity { get; set; }

    public int AvailableQuantity => TotalQuantity - ReservedQuantity;
}

public class SimpleStockItemViewModel
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}