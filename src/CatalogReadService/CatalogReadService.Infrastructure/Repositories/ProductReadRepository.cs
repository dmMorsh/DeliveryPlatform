using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.Models;
using CatalogReadService.Application.Queries.SearchProducts;
using CatalogReadService.Infrastructure.ReadStore;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Contracts;

namespace CatalogReadService.Infrastructure.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly ElasticsearchClient _es;
    private readonly IDatabase _cache;
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly RedisKey SearchVersionKey = "catalog:search:version";

    public ProductReadRepository(
        ElasticsearchClient es,
        IConnectionMultiplexer redis)
    {
        _es = es;
        _cache = redis.GetDatabase();
    }

    public async Task<PagedResult<ProductView>> SearchAsync(SearchProductsQuery request, CancellationToken ct)
    {
        var cacheKey = await BuildSearchCacheKeyAsync(request);
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<PagedResult<ProductView>>(cached.ToString()!)!;
        }

        var from = (request.Page - 1) * request.PageSize;

        // build a simple query object; avoid complex lambdas to keep overload resolution happy
        Query queryObj = string.IsNullOrWhiteSpace(request.Text)
            ? (Query)new MatchAllQuery()
            : new MultiMatchQuery
            {
                Fields = new[] { "name", "description" },
                Query = request.Text!,
                Type = TextQueryType.BestFields
            };

        var searchRequest = new SearchRequest("products")
        {
            Query = queryObj,
            From = from,
            Size = request.PageSize
        };

        var resp = await _es.SearchAsync<ProductReadModel>(searchRequest, ct);

        var items = resp.Hits.Select(h => new ProductView(
            Guid.Parse(h.Id),
            h.Source.Name,
            h.Source.Description,
            h.Source.PriceCents,
            "USD",
            0,
            h.Source.QuantityAvailable,
            h.Source.Category,
            h.Source.Colors,
            h.Source.Brand,
            h.Source.Rating
        )).ToList();

        var result = new PagedResult<ProductView>
        {
            Items = items,
            TotalCount = (int)resp.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), SearchCacheTtl);
        return result;
    }

    public async Task<ProductView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cacheKey = $"product:{id}";
        var cached = await _cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<ProductView>(cached.ToString()!)!;
        }

        var resp = await _es.GetAsync<ProductReadModel>(id.ToString(), g => g.Index("products"), ct);
        if (!resp.Found || resp.Source == null) return null;

        var view = new ProductView(
            resp.Source.Id,
            resp.Source.Name,
            resp.Source.Description,
            resp.Source.PriceCents,
            "USD",
            0,
            resp.Source.QuantityAvailable,
            resp.Source.Category,
            resp.Source.Colors,
            resp.Source.Brand,
            resp.Source.Rating
        );

        await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(view), TimeSpan.FromMinutes(10));
        return view;
    }

    private async Task<RedisKey> BuildSearchCacheKeyAsync(SearchProductsQuery request)
    {
        var version = await _cache.StringGetAsync(SearchVersionKey);
        var versionValue = version.HasValue ? version.ToString()! : "0";

        var normalized = string.Join("|", new[]
        {
            (request.Text ?? string.Empty).Trim(),
            request.CategoryId?.ToString() ?? string.Empty,
            request.MinPrice?.ToString() ?? string.Empty,
            request.MaxPrice?.ToString() ?? string.Empty,
            request.SortBy.ToString(),
            request.SortDirection.ToString(),
            request.Page.ToString(),
            request.PageSize.ToString()
        });

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"catalog:search:v{versionValue}:{hash}";
    }
}
