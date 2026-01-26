namespace CatalogService.Application.Models;

public record CreateProductModel(string Name, string? Description, decimal PriceCents, string? Currency);
