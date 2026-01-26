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
    /// Создать новый заказ
    /// </summary>
    /// <param name="request">Данные заказа</param>
    /// <returns>Созданный заказ</returns>
    [Obsolete]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Gateway: Creating order for client {ClientId}", request.ClientId);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "order-service", 
            "/api/orders", 
            request
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating order: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Получить заказ по ID
    /// </summary>
    /// <param name="orderId">ID заказа</param>
    /// <returns>Данные заказа</returns>
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        _logger.LogInformation("Gateway: Getting order {OrderId}", orderId);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "order-service",
            $"/api/orders/{orderId}"
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
    /// Получить заказы с фильтрацией и пагинацией
    /// </summary>
    /// <param name="clientId">Фильтр по ID клиента (опционально)</param>
    /// <param name="courierId">Фильтр по ID курьера (опционально)</param>
    /// <param name="status">Фильтр по статусу (опционально)</param>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <returns>Список заказов</returns>
    [HttpGet]
    public async Task<IActionResult> GetOrders(
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
            "order-service",
            path
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting orders: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Обновить заказ (назначить курьера, изменить статус)
    /// </summary>
    /// <param name="orderId">ID заказа</param>
    /// <param name="request">Данные для обновления</param>
    /// <returns>Обновленный заказ</returns>
    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] UpdateOrderRequest request)
    {
        _logger.LogInformation("Gateway: Updating order {OrderId}", orderId);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "order-service",
            $"/api/orders/{orderId}",
            request
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