using CatalogService.Application.Models;
using CatalogService.Domain.Aggregates;

namespace CatalogService.Application.Mapping;

public static class ProductViewFactory
{
    public static ProductView FromProduct(Product product)
    {
        return new ProductView(
            product.Id,
            product.Name,
            product.Description,
            product.PriceCents.AmountCents,
            product.PriceCents.Currency,
            product.WeightGrams.Value);
    }
}