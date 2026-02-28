using System;

namespace InventoryService.Infrastructure.ReadStore;

public class StockItemReadModel
{
    public Guid ProductId { get; set; }
    public int TotalQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}
