using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/product")]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly IProxyService _proxyService;

    public CatalogController(ILogger<CatalogController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> Search(CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Searching products");
        
        var queryString = HttpContext.Request.QueryString.Value;
        
        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-read-service",
            $"/api/catalog/search{queryString}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error searching product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting product {id}", id);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-read-service",
            $"/api/catalog/{id}",
            HttpContext,
            ct);
        if (statusCode >= 200 && statusCode < 300)
        { 
            return StatusCode(statusCode, data);
        }
        _logger.LogError("Error getting product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}
