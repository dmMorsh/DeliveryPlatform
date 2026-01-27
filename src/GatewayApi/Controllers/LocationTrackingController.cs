using Microsoft.AspNetCore.Mvc;
using Shared.Services;

namespace GatewayApi.Controllers;

/// <summary>
/// Proxy контроллер для Location Tracking через GatewayApi
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationTrackingController : ControllerBase
{
    private readonly ILocationTrackingClient _locationClient;
    private readonly ILogger<LocationTrackingController> _logger;

    public LocationTrackingController(ILocationTrackingClient locationClient, ILogger<LocationTrackingController> logger)
    {
        _locationClient = locationClient;
        _logger = logger;
    }

    /// <summary>
    /// Обновить локацию курьера
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="courierId">ID курьера</param>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="accuracy">Точность в метрах</param>
    [HttpPost("couriers/{courierId:guid}/location")]
    public async Task<IActionResult> UpdateCourierLocation(
        CancellationToken ct,
        Guid courierId,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int accuracy = 0)
    {
        try
        {
            _logger.LogInformation(
                "Updating location for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, latitude, longitude);

            var success = await _locationClient.UpdateCourierLocationAsync(courierId, latitude, longitude, accuracy);

            if (!success)
                return BadRequest(new { message = "Failed to update location" });

            return Ok(new
            {
                courierId,
                latitude,
                longitude,
                accuracy,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier location");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить текущую локацию курьера
    /// </summary>
    /// <param name="courierId">ID курьера</param>
    /// <param name="ct"></param>
    [HttpGet("couriers/{courierId:guid}/location")]
    public async Task<IActionResult> GetCourierLocation(Guid courierId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting location for courier {CourierId}", courierId);

            var (latitude, longitude, isOnline) = await _locationClient.GetCourierLocationAsync(courierId);

            return Ok(new
            {
                courierId,
                latitude,
                longitude,
                isOnline,
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courier location");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Получить локацию нескольких курьеров
    /// </summary>
    /// <param name="courierIds">Список ID курьеров (comma-separated)</param>
    /// <param name="ct"></param>
    [HttpGet("couriers/locations")]
    public async Task<IActionResult> GetCouriersLocations([FromQuery] string courierIds, CancellationToken ct)
    {
        try
        {
            var ids = courierIds.Split(',')
                .Where(id => Guid.TryParse(id.Trim(), out _))
                .Select(id => Guid.Parse(id.Trim()))
                .ToList();

            _logger.LogInformation("Getting locations for {Count} couriers", ids.Count);

            var locations = new List<object>();
            foreach (var id in ids)
            {
                var (latitude, longitude, isOnline) = await _locationClient.GetCourierLocationAsync(id);
                locations.Add(new { courierId = id, latitude, longitude, isOnline });
            }

            return Ok(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting couriers locations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
