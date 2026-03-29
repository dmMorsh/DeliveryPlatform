using GatewayApi.Contracts.Orders;
using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IProxyService _proxyService;

    public OrdersController(ILogger<OrdersController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="ct"></param>
    /// <returns>Order data</returns>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Getting order {OrderId}", orderId);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "order-read-service",
            $"/api/orders/{orderId}",
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        if (statusCode == 404)
            return NotFound(new ProxyErrorResponse { Message = "Order not found" });

        _logger.LogError("Error getting order: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Get orders with filtering and pagination
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="clientId">Filter by client ID (optional)</param>
    /// <param name="courierId">Filter by courier ID (optional)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of orders</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        CancellationToken ct,
        [FromQuery] int? clientId = null,
        [FromQuery] int? courierId = null,
        [FromQuery] int? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Gateway: Getting orders with filters - ClientId: {ClientId}, CourierId: {CourierId}, Status: {Status}", 
            clientId, courierId, status);

        var queryParams = new List<string>();
        if (clientId.HasValue) queryParams.Add($"clientId={clientId}");
        if (courierId.HasValue) queryParams.Add($"courierId={courierId}");
        if (status.HasValue) queryParams.Add($"status={status}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var path = "/api/orders?" + string.Join("&", queryParams);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "order-read-service",
            path,
            HttpContext,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting orders: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    
    /// <summary>
    /// Create a new order
    /// </summary>
    /// <param name="request">Order data</param>
    /// <param name="ct"></param>
    /// <returns>Created order</returns>
    [Obsolete]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Creating order for client {ClientId}", request.ClientId);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "order-service", 
            "/api/orders", 
            HttpContext,
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating order: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Update order (assign courier, change status)
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="request">Update data</param>
    /// <param name="ct"></param>
    /// <returns>Updated order</returns>
    [Obsolete]
    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] UpdateOrderRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Updating order {OrderId}", orderId);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "order-service",
            $"/api/orders/{orderId}",
            HttpContext,
            request, 
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        if (statusCode == 404)
            return NotFound(new ProxyErrorResponse { Message = "Order not found" });

        _logger.LogError("Error updating order: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}
