using Grpc.Net.Client;
using LocationTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

public class LocationTrackingClientImpl : ILocationTrackingClient
{
    private readonly ILogger<LocationTrackingClientImpl> _logger;
    private readonly string _serviceUrl;
    private LocationTrackingService.LocationTrackingServiceClient? _client;

    public LocationTrackingClientImpl(IConfiguration config, ILogger<LocationTrackingClientImpl> logger)
    {
        _logger = logger;
        _serviceUrl = config["gRPC:LocationTrackingService:Url"] ?? "https://localhost:7070";
    }

    private LocationTrackingService.LocationTrackingServiceClient GetClient()
    {
        if (_client != null)
            return _client;

        var channel = GrpcChannel.ForAddress(_serviceUrl);
        _client = new LocationTrackingService.LocationTrackingServiceClient(channel);
        return _client;
    }

    public async Task<bool> UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy = 0)
    {
        try
        {
            var client = GetClient();
            var request = new UpdateLocationRequest
            {
                CourierId = courierId.ToString(),
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await client.UpdateLocationAsync(request);

            _logger.LogInformation(
                "Updated location for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, latitude, longitude);

            return response.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for courier {CourierId}", courierId);
            return false;
        }
    }

    public async Task<(double Latitude, double Longitude, bool IsOnline)> GetCourierLocationAsync(Guid courierId)
    {
        try
        {
            var client = GetClient();
            var request = new GetLocationRequest { CourierId = courierId.ToString() };
            var response = await client.GetCourierLocationAsync(request);

            _logger.LogInformation(
                "Retrieved location for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, response.Latitude, response.Longitude);

            return (response.Latitude, response.Longitude, response.Status == 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for courier {CourierId}", courierId);
            return (0, 0, false);
        }
    }
}
