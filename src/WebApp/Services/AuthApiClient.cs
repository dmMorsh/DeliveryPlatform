using WebApp.Models;

namespace WebApp.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> LoginAsync(LoginViewModel model, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/auth/login",
            model,
            ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(ct);
        return result?.AccessToken;
    }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
}
