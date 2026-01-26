namespace InventoryService.Application.Models;

public record StockView
{
    public Guid ProductId { get; set; }
    public int AvailableQuantity { get; set; }
}