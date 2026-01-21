using System.Text.Json;

namespace GatewayApi.Services;

/// <summary>
/// Сервис для проксирования запросов к микросервисам
/// </summary>
public interface IProxyService
{
    Task<(T? Data, int StatusCode, string? Error)> ProxyPostAsync<T>(string serviceName, string path, object body);
    Task<(T? Data, int StatusCode, string? Error)> ProxyGetAsync<T>(string serviceName, string path);
    Task<(T? Data, int StatusCode, string? Error)> ProxyPutAsync<T>(string serviceName, string path, object body);
}

public class ProxyService : IProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyService> _logger;
    private readonly Dictionary<string, string> _serviceUrls;

    public ProxyService(IHttpClientFactory httpClientFactory, ILogger<ProxyService> logger, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Получаем URLs сервисов из конфигурации
        _serviceUrls = new Dictionary<string, string>
        {
            ["order-service"] = config["Services:OrderServiceUrl"] ?? "http://localhost:5001",
            ["courier-service"] = config["Services:CourierServiceUrl"] ?? "http://localhost:5002",
            ["location-tracking"] = config["Services:LocationTrackingUrl"] ?? "http://localhost:5003"
        };
    }

    public async Task<(T? Data, int StatusCode, string? Error)> ProxyPostAsync<T>(string serviceName, string path, object body)
    {
        try
        {
            if (!_serviceUrls.TryGetValue(serviceName, out var baseUrl))
            {
                _logger.LogError("Service {ServiceName} not found in configuration", serviceName);
                return (default, 500, $"Service {serviceName} not found");
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl}{path}";
            
            _logger.LogInformation("Proxying POST request to {Url}", url);

            var response = await client.PostAsJsonAsync(url, body);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (data, (int)response.StatusCode, null);
            }

            _logger.LogWarning("Service {ServiceName} returned status {StatusCode}: {Content}", serviceName, response.StatusCode, content);
            return (default, (int)response.StatusCode, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling {ServiceName}", serviceName);
            return (default, 503, $"Service {serviceName} unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            return (default, 500, ex.Message);
        }
    }

    public async Task<(T? Data, int StatusCode, string? Error)> ProxyGetAsync<T>(string serviceName, string path)
    {
        try
        {
            if (!_serviceUrls.TryGetValue(serviceName, out var baseUrl))
            {
                _logger.LogError("Service {ServiceName} not found in configuration", serviceName);
                return (default, 500, $"Service {serviceName} not found");
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl}{path}";

            _logger.LogInformation("Proxying GET request to {Url}", url);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (content == "Healthy") return (default, (int)response.StatusCode, null);
                var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (data, (int)response.StatusCode, null);
            }

            _logger.LogWarning("Service {ServiceName} returned status {StatusCode}: {Content}", serviceName, response.StatusCode, content);
            return (default, (int)response.StatusCode, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling {ServiceName}", serviceName);
            return (default, 503, $"Service {serviceName} unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            return (default, 500, ex.Message);
        }
    }

    public async Task<(T? Data, int StatusCode, string? Error)> ProxyPutAsync<T>(string serviceName, string path, object body)
    {
        try
        {
            if (!_serviceUrls.TryGetValue(serviceName, out var baseUrl))
            {
                _logger.LogError("Service {ServiceName} not found in configuration", serviceName);
                return (default, 500, $"Service {serviceName} not found");
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{baseUrl}{path}";

            _logger.LogInformation("Proxying PUT request to {Url}", url);

            var response = await client.PutAsJsonAsync(url, body);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return (data, (int)response.StatusCode, null);
            }

            _logger.LogWarning("Service {ServiceName} returned status {StatusCode}: {Content}", serviceName, response.StatusCode, content);
            return (default, (int)response.StatusCode, content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling {ServiceName}", serviceName);
            return (default, 503, $"Service {serviceName} unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            return (default, 500, ex.Message);
        }
    }
}
