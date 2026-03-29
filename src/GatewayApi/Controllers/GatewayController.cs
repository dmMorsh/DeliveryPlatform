using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

/// <summary>
/// Gateway Controller - single entry point for all clients
/// Proxies requests to OrderService and CourierService microservices
/// </summary>
[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly IProxyService _proxyService;

    public GatewayController(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    /// <summary>
    /// Gateway API health check
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
    /// Health check for all microservices
    /// </summary>
    [HttpGet("services-health")]
    public async Task<IActionResult> ServicesHealth(CancellationToken ct)
    {
        var services = new Dictionary<string, object>();

        services["order-service"] = await CheckServiceHealth("order-service", "/health", ct);
        services["courier-service"] = await CheckServiceHealth("courier-service", "/health", ct);

        return Ok(new { timestamp = DateTime.UtcNow, services });
    }

    private async Task<object> CheckServiceHealth(string serviceName, string path, CancellationToken ct)
    {
        var (_, statusCode, error) = await _proxyService.ProxyGetAsync<object>(serviceName, path, HttpContext, ct);
        return statusCode == 200 
            ? new { status = "up", code = statusCode }
            : new { status = "down", code = statusCode, error };
    }
}
