using Microsoft.AspNetCore.Mvc;
using GatewayApi.Services;

namespace GatewayApi.Controllers;

/// <summary>
/// Gateway Controller - единая точка входа для всех клиентов
/// Проксирует запросы к микросервисам OrderService и CourierService
/// </summary>
[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;
    private readonly IProxyService _proxyService;

    public GatewayController(ILogger<GatewayController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Проверка здоровья Gateway API
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            service = "GatewayApi",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Проверка здоровья всех микросервисов
    /// </summary>
    [HttpGet("services-health")]
    public async Task<IActionResult> ServicesHealth()
    {
        var services = new Dictionary<string, object>();

        services["order-service"] = await CheckServiceHealth("order-service", "/health");
        services["courier-service"] = await CheckServiceHealth("courier-service", "/health");

        return Ok(new { timestamp = DateTime.UtcNow, services });
    }

    private async Task<object> CheckServiceHealth(string serviceName, string path)
    {
        var (_, statusCode, error) = await _proxyService.ProxyGetAsync<object>(serviceName, path);
        return statusCode == 200 
            ? new { status = "up", code = statusCode }
            : new { status = "down", code = statusCode, error };
    }
}
