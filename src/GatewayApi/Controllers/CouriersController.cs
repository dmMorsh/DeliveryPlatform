using GatewayApi.Contracts.Couriers;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouriersController : ControllerBase
{
    private readonly ILogger<CouriersController> _logger;
    private readonly IProxyService _proxyService;

    public CouriersController(ILogger<CouriersController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Register a new courier
    /// </summary>
    /// <param name="request">Courier data</param>
    /// <param name="ct"></param>
    /// <returns>Created courier</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Creating courier {Phone}", request.Phone);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "courier-service",
            "/api/couriers",
            HttpContext,
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating courier: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Get courier data
    /// </summary>
    /// <param name="courierId">Courier ID</param>
    /// <param name="ct"></param>
    /// <returns>Courier data</returns>
    [HttpGet("{courierId}")]
    public async Task<IActionResult> GetCourier(int courierId, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting courier {CourierId}", courierId);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            $"/api/couriers/{courierId}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        if (statusCode == 404)
            return NotFound(new ProxyErrorResponse { Message = "Courier not found" });

        _logger.LogError("Error getting courier: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Get the list of active couriers (sorted by rating)
    /// </summary>
    /// <returns>List of active couriers</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCouriers(CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting active couriers");

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            "/api/couriers/active",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting active couriers: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Get all couriers with pagination
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="ct"></param>
    /// <returns>List of couriers</returns>
    [HttpGet]
    public async Task<IActionResult> GetCouriers(
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
        )
    {
        _logger.LogInformation("Gateway: Getting couriers - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            $"/api/couriers?page={page}&pageSize={pageSize}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting couriers: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Update courier data (status, location, rating)
    /// </summary>
    /// <param name="courierId">Courier ID</param>
    /// <param name="request">Update data</param>
    /// <param name="ct"></param>
    /// <returns>Updated courier</returns>
    [HttpPut("{courierId}")]
    public async Task<IActionResult> UpdateCourier(int courierId, [FromBody] UpdateCourierRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Updating courier {CourierId}", courierId);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "courier-service",
            $"/api/couriers/{courierId}",
            HttpContext, 
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        if (statusCode == 404)
            return NotFound(new ProxyErrorResponse { Message = "Courier not found" });

        _logger.LogError("Error updating courier: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}
