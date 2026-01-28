namespace CatalogService.Application.Models;

public record UpdateProductModel(string? Name, string? Description, long? PriceCents, string? Currency, bool? IsActive);