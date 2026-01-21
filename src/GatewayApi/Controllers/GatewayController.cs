using Microsoft.AspNetCore.Mvc;
using GatewayApi.DTOs;
using GatewayApi.Services;

namespace GatewayApi.Controllers;

/// <summary>
/// Gateway Controller - единая точка входа для всех клиентов
/// Проксирует запросы к микросервисам OrderService и CourierService
/// </summary>
[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly ILogger<GatewayController> _logger;
    private readonly IProxyService _proxyService;

    public GatewayController(ILogger<GatewayController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }

    /// <summary>
    /// Проверка здоровья Gateway API
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
    /// Проверка здоровья всех микросервисов
    /// </summary>
    [HttpGet("services-health")]
    public async Task<IActionResult> ServicesHealth()
    {
        var services = new Dictionary<string, object>();

        services["order-service"] = await CheckServiceHealth("order-service", "/health");
        services["courier-service"] = await CheckServiceHealth("courier-service", "/health");

        return Ok(new { timestamp = DateTime.UtcNow, services });
    }

    private async Task<object> CheckServiceHealth(string serviceName, string path)
    {
        var (_, statusCode, error) = await _proxyService.ProxyGetAsync<object>(serviceName, path);
        return statusCode == 200 
            ? new { status = "up", code = statusCode }
            : new { status = "down", code = statusCode, error };
    }

    #region Orders Management

    /// <summary>
    /// Создать новый заказ
    /// </summary>
    /// <param name="request">Данные заказа</param>
    /// <returns>Созданный заказ</returns>
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderProxyDto request)
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
    [HttpGet("orders/{orderId}")]
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
    [HttpGet("orders")]
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
    [HttpPut("orders/{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] UpdateOrderProxyDto request)
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

    #endregion

    #region Couriers Management

    /// <summary>
    /// Зарегистрировать нового курьера
    /// </summary>
    /// <param name="request">Данные курьера</param>
    /// <returns>Созданный курьер</returns>
    [HttpPost("couriers")]
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierProxyDto request)
    {
        _logger.LogInformation("Gateway: Creating courier {Phone}", request.Phone);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "courier-service",
            "/api/couriers",
            request
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error creating courier: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Получить данные курьера
    /// </summary>
    /// <param name="courierId">ID курьера</param>
    /// <returns>Данные курьера</returns>
    [HttpGet("couriers/{courierId}")]
    public async Task<IActionResult> GetCourier(int courierId)
    {
        _logger.LogInformation("Gateway: Getting courier {CourierId}", courierId);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            $"/api/couriers/{courierId}"
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
    /// Получить список активных курьеров (отсортированы по рейтингу)
    /// </summary>
    /// <returns>Список активных курьеров</returns>
    [HttpGet("couriers/active")]
    public async Task<IActionResult> GetActiveCouriers()
    {
        _logger.LogInformation("Gateway: Getting active couriers");

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            "/api/couriers/active"
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting active couriers: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Получить всех курьеров с пагинацией
    /// </summary>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <returns>Список курьеров</returns>
    [HttpGet("couriers")]
    public async Task<IActionResult> GetCouriers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Gateway: Getting couriers - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var (data, statusCode, error) = await _proxyService.ProxyGetAsync<dynamic>(
            "courier-service",
            $"/api/couriers?page={page}&pageSize={pageSize}"
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return Ok(data);
        }

        _logger.LogError("Error getting couriers: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }

    /// <summary>
    /// Обновить данные курьера (статус, локация, рейтинг)
    /// </summary>
    /// <param name="courierId">ID курьера</param>
    /// <param name="request">Данные для обновления</param>
    /// <returns>Обновленный курьер</returns>
    [HttpPut("couriers/{courierId}")]
    public async Task<IActionResult> UpdateCourier(int courierId, [FromBody] UpdateCourierProxyDto request)
    {
        _logger.LogInformation("Gateway: Updating courier {CourierId}", courierId);

        var (data, statusCode, error) = await _proxyService.ProxyPutAsync<dynamic>(
            "courier-service",
            $"/api/couriers/{courierId}",
            request
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

    #endregion
}
