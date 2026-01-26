namespace InventoryService.Application.Models;

public record ReserveStockModel(Guid OrderId, int Quantity);