using Grpc.Core;
using LocationTracking;

namespace LocationTrackingService.Services;

/// <summary>
/// gRPC service for courier location tracking
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
    /// Get current courier location
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
    /// Update courier location (single request)
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

    /// <summary>
    /// Update courier location (streaming)
    /// </summary>
    public override async Task StreamLocation(
        IAsyncStreamReader<UpdateLocationRequest> requestStream,
        IServerStreamWriter<LocationUpdate> responseStream,
        ServerCallContext context)
    {
        try
        {
            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                _logger.LogInformation(
                    "Stream location update: Courier {CourierId} at ({Latitude}, {Longitude})",
                    request.CourierId, request.Latitude, request.Longitude);

                await _locationService.UpdateCourierLocationAsync(
                    Guid.Parse(request.CourierId),
                    request.Latitude,
                    request.Longitude,
                    request.Accuracy,
                    DateTimeOffset.FromUnixTimeMilliseconds(request.TimestampMs));

                await responseStream.WriteAsync(new LocationUpdate
                {
                    Success = true,
                    Message = "Location updated successfully",
                    CourierId = request.CourierId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StreamLocation");
        }
    }

    /// <summary>
    /// Get courier location history
    /// </summary>
    public override async Task<CourierLocationHistory> GetCourierLocationHistory(
        GetLocationHistoryRequest request,
        ServerCallContext context)
    {
        var courierId = Guid.Parse(request.CourierId);
        var from = request.FromTimestampMs > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(request.FromTimestampMs)
            : (DateTimeOffset?)null;

        var limit = request.Limit <= 0 ? 100 : request.Limit;
        var history = await _locationService.GetCourierLocationHistoryAsync(courierId, from, limit);

        var response = new CourierLocationHistory { CourierId = request.CourierId };
        response.Points.AddRange(history.Select(h => new LocationPoint
        {
            Latitude = h.Latitude,
            Longitude = h.Longitude,
            TimestampMs = h.UpdatedAt.ToUnixTimeMilliseconds(),
            Accuracy = h.Accuracy
        }));

        return response;
    }
}
