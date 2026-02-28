namespace CatalogService.Application.Interfaces;

public interface ICatalogMetricsStore
{
    Task IncrementProductSalesAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task IncrementReservedQuantityAsync(Guid productId, int quantity, CancellationToken ct = default);
}
