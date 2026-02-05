using GatewayApi.Contracts.Inventory;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ILogger<InventoryController> _logger;
    private readonly IProxyService _proxyService;

    public InventoryController(ILogger<InventoryController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetStocks(CancellationToken ct)
    {
        _logger.LogInformation("Gateway: get stocks");

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "inventory-service",
            "/api/stock",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating stock: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
    
    [HttpPost]
    public async Task<IActionResult> AddStocks([FromBody] AddStockRequest[] request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Adding items");

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "inventory-service",
            "/api/stock",
            HttpContext,
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating stocks: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
    
    [HttpPut]
    public async Task<IActionResult> AdjustStocks([FromBody] AddStockRequest[] request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Adjusting items");

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "inventory-service",
            "/api/stock",
            HttpContext,
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error adjusting stocks: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

}