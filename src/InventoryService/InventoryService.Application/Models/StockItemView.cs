namespace InventoryService.Application.Models;

public record StockItemView
{
    public Guid ProductId { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}