namespace CatalogService.Application.Models;

public record ProductView(Guid Id, string Name, string Description, long PriceCents, int StockQuantity, DateTime CreatedAt);