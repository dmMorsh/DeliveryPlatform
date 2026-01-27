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
    /// Зарегистрировать нового курьера
    /// </summary>
    /// <param name="request">Данные курьера</param>
    /// <param name="ct"></param>
    /// <returns>Созданный курьер</returns>
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
    /// Получить данные курьера
    /// </summary>
    /// <param name="courierId">ID курьера</param>
    /// <param name="ct"></param>
    /// <returns>Данные курьера</returns>
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
    /// Получить список активных курьеров (отсортированы по рейтингу)
    /// </summary>
    /// <returns>Список активных курьеров</returns>
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
    /// Получить всех курьеров с пагинацией
    /// </summary>
    /// <param name="page">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="ct"></param>
    /// <returns>Список курьеров</returns>
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
    /// Обновить данные курьера (статус, локация, рейтинг)
    /// </summary>
    /// <param name="courierId">ID курьера</param>
    /// <param name="request">Данные для обновления</param>
    /// <param name="ct"></param>
    /// <returns>Обновленный курьер</returns>
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