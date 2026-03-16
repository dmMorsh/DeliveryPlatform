using CatalogReadService.Application.Models;
using Elastic.Clients.Elasticsearch;
using Shared.Contracts.Events;
using StackExchange.Redis;

namespace CatalogReadService.Infrastructure.Services;

public sealed class ProductReadProjector
{
    private const string IndexName = "products";
    private static readonly RedisKey SearchVersionKey = "catalog:search:version";
    private readonly ElasticsearchClient _es;
    private readonly IConnectionMultiplexer _redis;

    public ProductReadProjector(ElasticsearchClient es, IConnectionMultiplexer redis)
    {
        _es = es;
        _redis = redis;
    }

    public async Task HandleAsync(ProductCreatedEvent evt, CancellationToken ct)
    {
        if (evt.ProductId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(evt.ProductId));
        if (string.IsNullOrWhiteSpace(evt.Name))
            throw new ArgumentException("Name cannot be empty", nameof(evt.Name));
        if (evt.PriceCents < 0)
            throw new ArgumentException("Price cannot be negative", nameof(evt.PriceCents));

        var doc = new ProductReadModel
        {
            Id = evt.ProductId,
            Name = evt.Name,
            Description = evt.Description,
            PriceCents = evt.PriceCents,
            Currency = evt.Currency,
            WeightGrams = evt.WeightGrams,
            QuantityAvailable = evt.QuantityAvailable,
            UpdatedAt = evt.Timestamp
        };

        try
        {
            await EnsureIndexExistsAsync(ct);
            await _es.IndexAsync(doc, i => i.Index(IndexName).Id(evt.ProductId.ToString()), ct);
            await _redis.GetDatabase().StringIncrementAsync(SearchVersionKey);
        }
        finally
        {
            // Always clear cache, even if ES fails
            await _redis.GetDatabase().KeyDeleteAsync(CacheKey(evt.ProductId));
        }
    }

    public async Task HandleAsync(ProductPriceChangedEvent evt, CancellationToken ct)
    {
        if (evt.NewPriceCents < 0)
            throw new ArgumentException("Price cannot be negative", nameof(evt.NewPriceCents));

        await EnsureIndexExistsAsync(ct);
        // construct an explicit request to avoid overload confusion
        var updateReq = new UpdateRequest<ProductReadModel, object>(IndexName, evt.ProductId.ToString())
        {
            Doc = new { PriceCents = evt.NewPriceCents, UpdatedAt = DateTime.UtcNow }
        };
        await _es.UpdateAsync(updateReq, ct);
        await _redis.GetDatabase().StringIncrementAsync(SearchVersionKey);
        await _redis.GetDatabase().KeyDeleteAsync(CacheKey(evt.ProductId));
    }

    public async Task HandleAsync(StockQuantityChangedEvent evt, CancellationToken ct)
    {
        await EnsureIndexExistsAsync(ct);
        var updateReq = new UpdateRequest<ProductReadModel, object>(IndexName, evt.ProductId.ToString())
        {
            Doc = new
            {
                QuantityAvailable = evt.AvailableQuantity,
                UpdatedAt = evt.Timestamp
            }
        };
        await _es.UpdateAsync(updateReq, ct);
        await _redis.GetDatabase().StringIncrementAsync(SearchVersionKey);
        await _redis.GetDatabase().KeyDeleteAsync(CacheKey(evt.ProductId));
    }

    private static RedisKey CacheKey(Guid id) => $"product:{id}";

    private async Task EnsureIndexExistsAsync(CancellationToken ct)
    {
        var exists = await _es.Indices.ExistsAsync(IndexName, ct);
        if (!exists.Exists)
        {
            // create index without explicit mappings to avoid API changes
            await _es.Indices.CreateAsync(IndexName, ct);
        }
    }
}
