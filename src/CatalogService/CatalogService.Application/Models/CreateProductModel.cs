namespace CatalogService.Application.Models;

public record CreateProductModel(string Name, string Description, long PriceCents, int StockQuantity);
