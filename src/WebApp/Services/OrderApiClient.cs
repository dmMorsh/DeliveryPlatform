using Shared.Utilities;
using WebApp.Models;

namespace WebApp.Services;

public class OrderApiClient
{
    private readonly HttpClient _http;

    public OrderApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<OrderViewModel>?> GetMyOrdersAsync(CancellationToken ct)
    {
        try
        {
            var res = await _http.GetFromJsonAsync<ApiResponse<List<OrderViewModel>>?>(
                "/api/orders",
                ct);
            return res.Data;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<OrderViewModel?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var res = await _http.GetFromJsonAsync<ApiResponse<OrderViewModel>>(
            $"/api/orders/{id}",
            ct);
        return res.Data;
    }
}
