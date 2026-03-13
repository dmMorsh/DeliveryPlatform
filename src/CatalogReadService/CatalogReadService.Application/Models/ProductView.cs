namespace CatalogReadService.Application.Models;

public record ProductView(
    Guid Id,
    string Name,
    string? Description,
    long PriceCents,
    string Currency,
    long WeightGrams,
    int QuantityAvailable = 0,
    string? Category = null,
    string[]? Colors = null,
    string? Brand = null,
    double? Rating = null
);
