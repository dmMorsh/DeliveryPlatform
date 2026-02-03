using Shared.Utilities;
using WebApp.Models;

namespace WebApp.Services;

public class CatalogApiClient
{
    private readonly HttpClient _http;

    public CatalogApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ProductViewModel>> GetProductsAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/product/search", ct);
        response.EnsureSuccessStatusCode();

        var res = await response.Content
                   .ReadFromJsonAsync<ApiResponse<PagedResult<ProductViewModel>>>(cancellationToken: ct);
        
        return res?.Data?.Items.ToList() ?? new List<ProductViewModel>();
    }

    public async Task<ProductViewModel> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/product/{id}", ct);
        response.EnsureSuccessStatusCode();

        var res = (await response.Content
            .ReadFromJsonAsync<ApiResponse<ProductViewModel>>(cancellationToken: ct))!;
        return res?.Data ?? new ProductViewModel();
    }

    public async Task AddAsync(ProductViewModel model, CancellationToken ct)
    {
        await _http.PostAsJsonAsync("/api/product", model, ct);
    }
}
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}