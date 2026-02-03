using Shared.Utilities;
using WebApp.Models;

namespace WebApp.Services;

public class CartApiClient
{
    private readonly HttpClient _http;

    public CartApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CartViewModel?> GetCartAsync(CancellationToken ct)
    {
        var res = await _http.GetFromJsonAsync<ApiResponse<CartViewModel>>(
            "/api/cart",
            ct);
        return res?.Data ?? new CartViewModel();
    }

    public async Task AddAsync(Guid productId, string name, long priceCents,
        int quantity, CancellationToken ct)
    {
        await _http.PostAsJsonAsync(
            "/api/cart/items",
            new { productId, name, priceCents, quantity},
            ct);
    }

    public async Task RemoveAsync(Guid productId, CancellationToken ct)
    {
        await _http.DeleteAsync(
            $"/api/cart/items/{productId}",
            ct);
    }
    
    public async Task<string?> CheckoutAsync(
        CheckoutViewModel model,
        CancellationToken ct)
    {
        var resp = await _http.PostAsJsonAsync(
            "/api/cart/checkout",
            model,
            ct);

        if (!resp.IsSuccessStatusCode)
            return null;

        var res = await resp.Content.ReadFromJsonAsync<OrderIdViewModel>(ct);
        return res?.OrderId;
    }
private record OrderIdViewModel(string OrderId);
}
