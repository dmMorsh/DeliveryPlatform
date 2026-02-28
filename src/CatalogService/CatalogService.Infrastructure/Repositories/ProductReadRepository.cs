using CatalogService.Application.Common;
using CatalogService.Application.Common.Enums;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using CatalogService.Application.Queries;
using CatalogService.Application.Queries.SearchProducts;
using CatalogService.Domain.Aggregates;
using CatalogService.Infrastructure.Persistence;
using CatalogService.Infrastructure.ReadStore;
using Microsoft.EntityFrameworkCore;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CatalogService.Infrastructure.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly CatalogDbContext _db;
    private readonly ElasticsearchClient _es;
    private readonly IDatabase _cache;

    public ProductReadRepository(CatalogDbContext db, ElasticsearchClient es, IConnectionMultiplexer redis)
    {
        _db = db;
        _es = es;
        _cache = redis.GetDatabase();
    }

    public async Task<PagedResult<ProductView>> SearchAsync(SearchProductsQuery request, CancellationToken ct)
    {
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

        return new PagedResult<ProductView>
        {
            Items = items,
            TotalCount = (int)resp.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };
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
        if (!resp.Found) return null;

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
}