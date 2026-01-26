using GatewayApi.Contracts.Catalog;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api")]
public class CatalogController : ControllerBase
{
    private readonly ILogger<CatalogController> _logger;
    private readonly IProxyService _proxyService;

    public CatalogController(ILogger<CatalogController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Зарегистрировать новый продукт
    /// </summary>
    /// <param name="request">Данные продукта</param>
    /// <param name="ct"></param>
    /// <returns>Созданный продукт</returns>
    [HttpPost("product")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateCatalogProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Creating product {product}", request.Name);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "catalog-service",
            "/api/catalog",
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

    /// <summary>
    /// Обновить продукт
    /// </summary>
    /// <param name="request">Данные продукта</param>
    /// <param name="ct"></param>
    /// <returns> продукт</returns>
    [HttpPut("product")]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateCatalogProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Updating product {product}", request.Name);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/{request.Id}",
            request, 
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error updating product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    [HttpGet("product/search")]
    public async Task<IActionResult> Search(CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Searching products");
        
        var queryString = HttpContext.Request.QueryString.Value;
        
        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/search{queryString}",
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error searching product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    [HttpGet("product/{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting product {id}", id);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/{id}",
            ct);
        if (statusCode >= 200 && statusCode < 300)
        { 
            return StatusCode(statusCode, data);
        }
        _logger.LogError("Error getting product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}