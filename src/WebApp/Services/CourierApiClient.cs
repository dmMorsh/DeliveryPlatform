using System.Net.Http.Json;
using Shared.Utilities;
using WebApp.Models;

namespace WebApp.Services;

public class CourierApiClient
{
    private readonly HttpClient _http;

    public CourierApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<object>?> RegisterAsync(CourierRegisterModel model, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync("/api/couriers", model, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> UpdateAsync(CourierUpdateModel model, CancellationToken ct)
    {
        var res = await _http.PutAsJsonAsync($"/api/couriers/{model.CourierId}", model, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }
}
