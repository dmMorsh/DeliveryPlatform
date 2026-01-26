using Grpc.Core;
using LocationTracking;

namespace LocationTrackingService.Services;

/// <summary>
/// gRPC сервис для трекинга локации курьеров
/// </summary>
public class LocationTrackingServiceImpl : LocationTracking.LocationTrackingService.LocationTrackingServiceBase
{
    private readonly ILogger<LocationTrackingServiceImpl> _logger;
    private readonly ILocationService _locationService;

    public LocationTrackingServiceImpl(ILogger<LocationTrackingServiceImpl> logger, ILocationService locationService)
    {
        _logger = logger;
        _locationService = locationService;
    }

    /// <summary>
    /// Получить текущую локацию курьера
    /// </summary>
    public override async Task<CourierLocation> GetCourierLocation(
        GetLocationRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Getting location for courier {CourierId}", request.CourierId);

            var location = await _locationService.GetCourierLocationAsync(Guid.Parse(request.CourierId));

            if (location == null)
            {
                return new CourierLocation
                {
                    CourierId = request.CourierId,
                    Status = 0 // offline
                };
            }

            return new CourierLocation
            {
                CourierId = request.CourierId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                LastUpdateMs = location.UpdatedAt.ToUnixTimeMilliseconds(),
                Status = 1 // online
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for courier {CourierId}", request.CourierId);
            throw;
        }
    }

    /// <summary>
    /// Обновить локацию курьера (single request)
    /// </summary>
    public override async Task<LocationUpdate> UpdateLocation(
        UpdateLocationRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "Received location update: Courier {CourierId} at ({Latitude}, {Longitude})",
                request.CourierId, request.Latitude, request.Longitude);

            await _locationService.UpdateCourierLocationAsync(
                Guid.Parse(request.CourierId),
                request.Latitude,
                request.Longitude,
                request.Accuracy,
                DateTimeOffset.FromUnixTimeMilliseconds(request.TimestampMs));

            return new LocationUpdate
            {
                Success = true,
                Message = "Location updated successfully",
                CourierId = request.CourierId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for courier {CourierId}", request.CourierId);
            return new LocationUpdate
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                CourierId = request.CourierId
            };
        }
    }
}
