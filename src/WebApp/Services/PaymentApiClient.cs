using Shared.Contracts;
using WebApp.Models;

namespace WebApp.Services;

public class PaymentApiClient
{
    private readonly HttpClient _http;

    public PaymentApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaymentStatusViewModel?> GetStatusAsync(Guid orderId, CancellationToken ct)
    {
        try
        {
            var res = await _http.GetFromJsonAsync<ApiResponse<PaymentStatusViewModel>>(
                $"/api/payments/status/{orderId}",
                ct);
            return res is { Success: true } ? res.Data : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StartPaymentResultViewModel?> StartPaymentAsync(
        Guid orderId,
        string provider,
        bool capture,
        CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                "/api/payments/start",
                new StartPaymentRequestModel(orderId, provider, capture),
                ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var res = await response.Content.ReadFromJsonAsync<ApiResponse<StartPaymentResultViewModel>>(cancellationToken: ct);
            return res is { Success: true } ? res.Data : null;
        }
        catch
        {
            return null;
        }
    }
}
