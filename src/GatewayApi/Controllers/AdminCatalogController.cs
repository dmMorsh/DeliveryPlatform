using GatewayApi.Contracts.Catalog;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/admin/product")]
public class AdminCatalogController : ControllerBase
{
    private readonly ILogger<AdminCatalogController> _logger;
    private readonly IProxyService _proxyService;

    public AdminCatalogController(ILogger<AdminCatalogController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Admin searching products");

        var queryString = HttpContext.Request.QueryString.Value;
        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/search{queryString}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
            return StatusCode(statusCode, data);

        _logger.LogError("Error searching admin catalog: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Admin getting product {id}", id);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/{id}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
            return StatusCode(statusCode, data);

        _logger.LogError("Error getting admin product: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Зарегистрировать новый продукт
    /// </summary>
    /// <param name="request">Данные продукта</param>
    /// <param name="ct"></param>
    /// <returns>Созданный продукт</returns>
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateCatalogProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Creating product {product}", request.Name);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "catalog-service",
            "/api/catalog",
            HttpContext,
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
    [HttpPut]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateCatalogProductRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Updating product {product}", request.Name);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "catalog-service",
            $"/api/catalog/{request.Id}",
            HttpContext,
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

    
    // [HttpPost]
    // public async Task<IActionResult> CreateProduct([FromBody] object request, CancellationToken ct)
    // {
    //     _logger.LogInformation("Gateway: Admin creating product");
    //
    //     var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
    //         "catalog-service",
    //         "/api/catalog",
    //         HttpContext,
    //         request,
    //         ct
    //     );
    //
    //     if (statusCode >= 200 && statusCode < 300)
    //         return StatusCode(statusCode, data);
    //
    //     _logger.LogError("Error creating admin product: {Error}", error);
    //     return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    // }
    //
    // [HttpPut("{id}")]
    // public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] object request, CancellationToken ct)
    // {
    //     _logger.LogInformation("Gateway: Admin updating product {id}", id);
    //
    //     var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
    //         "catalog-service",
    //         $"/api/catalog/{id}",
    //         HttpContext,
    //         request,
    //         ct
    //     );
    //
    //     if (statusCode >= 200 && statusCode < 300)
    //         return StatusCode(statusCode, data);
    //
    //     _logger.LogError("Error updating admin product: {Error}", error);
    //     return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    // }
}
