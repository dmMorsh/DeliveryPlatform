using System.Text.Json;
using CatalogService.Infrastructure.ReadStore;
using Elastic.Clients.Elasticsearch;
using Shared.Contracts.Events;
using StackExchange.Redis;

namespace CatalogService.Infrastructure.ReadStore;

public sealed class ProductReadProjector
{
    private const string IndexName = "products";
    private readonly ElasticsearchClient _es;
    private readonly IConnectionMultiplexer _redis;

    public ProductReadProjector(ElasticsearchClient es, IConnectionMultiplexer redis)
    {
        _es = es;
        _redis = redis;
    }

    public async Task HandleAsync(ProductCreatedEvent evt, CancellationToken ct)
    {
        var doc = new ProductReadModel
        {
            Id = evt.ProductId,
            Name = evt.Name,
            Description = evt.Description,
            PriceCents = evt.PriceCents,
            QuantityAvailable = evt.QuantityAvailable,
            UpdatedAt = evt.Timestamp
        };

        await EnsureIndexExistsAsync(ct);
        await _es.IndexAsync(doc, i => i.Index(IndexName).Id(evt.ProductId.ToString()), ct);
        await _redis.GetDatabase().KeyDeleteAsync(CacheKey(evt.ProductId));
    }

    public async Task HandleAsync(ProductPriceChangedEvent evt, CancellationToken ct)
    {
        await EnsureIndexExistsAsync(ct);
        // construct an explicit request to avoid overload confusion
        var updateReq = new UpdateRequest<ProductReadModel, object>(IndexName, evt.ProductId.ToString())
        {
            Doc = new { PriceCents = evt.NewPriceCents, UpdatedAt = DateTime.UtcNow }
        };
        await _es.UpdateAsync(updateReq, ct);
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
