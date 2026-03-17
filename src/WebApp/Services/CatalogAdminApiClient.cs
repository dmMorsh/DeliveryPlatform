using Shared.Contracts;
using WebApp.Models;

namespace WebApp.Services;

public class CatalogAdminApiClient
{
    private readonly HttpClient _http;

    public CatalogAdminApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ProductViewModel>> GetProductsAsync(CancellationToken ct)
    {
        var response = await _http.GetAsync("/api/admin/product/search", ct);
        response.EnsureSuccessStatusCode();

        var res = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<ProductViewModel>>>(cancellationToken: ct);

        return res?.Data?.Items.ToList() ?? new List<ProductViewModel>();
    }

    public async Task<ProductViewModel> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/admin/product/{id}", ct);
        response.EnsureSuccessStatusCode();

        var res = await response.Content
            .ReadFromJsonAsync<ApiResponse<ProductViewModel>>(cancellationToken: ct);
        return res?.Data ?? new ProductViewModel();
    }

    public async Task AddAsync(ProductViewModel model, CancellationToken ct)
    {
        await _http.PostAsJsonAsync("/api/admin/product", model, ct);
    }

    public async Task UpdateAsync(ProductViewModel model, CancellationToken ct)
    {
        await _http.PutAsJsonAsync("/api/admin/product", model, ct);
    }
}
