using Shared.Contracts;
using WebApp.Models;

namespace WebApp.Services;

public class DeliveryApiClient
{
    private readonly HttpClient _http;

    public DeliveryApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ApiResponse<object>?> GetByOrderAsync(Guid orderId, CancellationToken ct)
    {
        var res = await _http.GetFromJsonAsync<ApiResponse<object>>($"/api/deliveries/by-order/{orderId}", ct);
        return res;
    }

    public async Task<ApiResponse<object>?> GetByIdAsync(Guid deliveryId, CancellationToken ct)
    {
        var res = await _http.GetFromJsonAsync<ApiResponse<object>>($"/api/deliveries/{deliveryId}", ct);
        return res;
    }

    public async Task<ApiResponse<object>?> AcceptAsync(Guid deliveryId, Guid courierId, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/accept", new { courierId }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> DeclineAsync(Guid deliveryId, Guid courierId, string? reason, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/decline", new { courierId, reason }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> PickUpAsync(Guid deliveryId, Guid courierId, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/pickup", new { courierId }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> StartAsync(Guid deliveryId, Guid courierId, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/start", new { courierId }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> CompleteAsync(Guid deliveryId, DeliveryActionModel model, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/complete", new
        {
            courierId = model.CourierId,
            signature = model.Signature,
            photoUrl = model.PhotoUrl,
            notes = model.Notes,
            verificationCode = model.VerificationCode
        }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> FailAsync(Guid deliveryId, string? reason, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/fail", new { reason }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> ReturnAsync(Guid deliveryId, string? reason, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/return", new { reason }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }

    public async Task<ApiResponse<object>?> CancelAsync(Guid deliveryId, string? reason, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync($"/api/deliveries/{deliveryId}/cancel", new { reason }, ct);
        return await res.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: ct);
    }
}
