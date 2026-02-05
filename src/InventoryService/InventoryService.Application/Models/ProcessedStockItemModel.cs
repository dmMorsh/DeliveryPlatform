namespace InventoryService.Application.Models;

public record ProcessedStockItemModel(Guid ProductId, int Quantity, string Description);