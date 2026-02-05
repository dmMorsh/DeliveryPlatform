using Shared.Utilities;
using WebApp.Models;

namespace WebApp.Services;

public class InventoryApiClient
{
    private readonly HttpClient _http;

    public InventoryApiClient(HttpClient http)
    {
        _http = http;
    }
    
    public async Task<List<StockItemViewModel>?> GetStocksAsync(CancellationToken ct)
    {
        var res = await _http.GetFromJsonAsync<ApiResponse<List<StockItemViewModel>>>($"api/inventory", ct);
        return res?.Data;
    }

    public async Task AddStocksAsync(SimpleStockItemViewModel[] createModels, CancellationToken ct)
    {
        await _http.PostAsJsonAsync($"api/inventory", createModels, ct);
    }
    
    public async Task AdjustStocksAsync(SimpleStockItemViewModel[] createModels, CancellationToken ct)
    {
        await _http.PutAsJsonAsync($"api/inventory", createModels, ct);
    }
}