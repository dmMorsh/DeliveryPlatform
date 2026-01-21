using Grpc.Core;
using LocationTracking;

namespace LocationTrackingService.Services;

/// <summary>
/// gRPC сервис для streaming локаций курьеров (реализация proto)
/// </summary>
public class LocationTrackingGrpcService : LocationTracking.LocationTrackingService.LocationTrackingServiceBase
{
    private readonly ICourierLocationService _locationService;
    private readonly ILogger<LocationTrackingGrpcService> _logger;

    public LocationTrackingGrpcService(
        ICourierLocationService locationService,
        ILogger<LocationTrackingGrpcService> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    // bidirectional stream: receives UpdateLocationRequest and can respond with LocationUpdate
    public override async Task StreamLocation(IAsyncStreamReader<UpdateLocationRequest> requestStream, IServerStreamWriter<LocationUpdate> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("StreamLocation started");

        try
        {
            await foreach (var req in requestStream.ReadAllAsync(context.CancellationToken))
            {
                // parse courier id GUID
                if (!Guid.TryParse(req.CourierId, out var courierGuid))
                {
                    var err = new LocationUpdate
                    {
                        Success = false,
                        Message = "Invalid courier_id GUID",
                        CourierId = req.CourierId
                    };
                    await responseStream.WriteAsync(err);
                    continue;
                }

                // Update in-memory store
                await _locationService.UpdateLocationAsync(courierGuid, req.Latitude, req.Longitude);

                var ack = new LocationUpdate
                {
                    Success = true,
                    Message = "Location updated",
                    CourierId = req.CourierId
                };

                await responseStream.WriteAsync(ack);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("StreamLocation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StreamLocation");
            throw;
        }
    }

    public override async Task<CourierLocation> GetCourierLocation(GetLocationRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CourierId, out var courierGuid))
        {
            return new CourierLocation
            {
                CourierId = request.CourierId,
                Latitude = 0,
                Longitude = 0,
                LastUpdateMs = 0,
                Status = 0
            };
        }

        var data = await _locationService.GetLocationAsync(courierGuid);
        if (data == null)
        {
            return new CourierLocation
            {
                CourierId = request.CourierId,
                Latitude = 0,
                Longitude = 0,
                LastUpdateMs = 0,
                Status = 0
            };
        }

        return new CourierLocation
        {
            CourierId = data.CourierId.ToString(),
            Latitude = data.Latitude,
            Longitude = data.Longitude,
            LastUpdateMs = new DateTimeOffset(data.LastUpdateAt).ToUnixTimeMilliseconds(),
            Status = data.Status
        };
    }
}
