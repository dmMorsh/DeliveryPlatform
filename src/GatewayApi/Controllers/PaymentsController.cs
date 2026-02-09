using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IProxyService _proxyService;

    public PaymentsController(ILogger<PaymentsController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    [HttpGet("status/{orderId:guid}")]
    public async Task<IActionResult> GetStatus(Guid orderId, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting payment status for order {OrderId}", orderId);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "payment-service",
            $"/api/payment/status/{orderId}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
            return Ok(data);

        if (statusCode == 404)
            return NotFound(new ProxyErrorResponse { Message = "Payment not found" });

        _logger.LogError("Error getting payment status: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] object body, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Starting payment");

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "payment-service",
            "/api/payment/start",
            HttpContext,
            body,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
            return Ok(data);

        _logger.LogError("Error starting payment: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}
