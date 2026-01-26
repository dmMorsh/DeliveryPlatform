using GatewayApi.Contracts.Cart;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ILogger<CartController> _logger;
    private readonly IProxyService _proxyService;

    public CartController(ILogger<CartController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Adding item {item}", request.Name);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "cart-service",
            "/api/cart",
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}