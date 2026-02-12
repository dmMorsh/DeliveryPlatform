namespace CatalogService.Api.Contracts;

public record CreateProductRequest(string Name, string? Description, long PriceCents, string? Currency, long WeightGrams);
