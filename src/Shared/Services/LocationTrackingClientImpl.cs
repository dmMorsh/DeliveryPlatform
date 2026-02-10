using Grpc.Net.Client;
using LocationTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Shared.Services;

public class LocationTrackingClientImpl : ILocationTrackingClient
{
    private readonly ILogger<LocationTrackingClientImpl> _logger;
    private readonly string _serviceUrl;
    private readonly IAsyncPolicy _policy;
    private LocationTrackingService.LocationTrackingServiceClient? _client;

    public LocationTrackingClientImpl(IConfiguration config, IHostEnvironment env, ILogger<LocationTrackingClientImpl> logger)
    {
        _logger = logger;
        _serviceUrl = ConfigurationGuard.GetRequired(config, env, "gRPC:LocationTrackingService:Url", "https://localhost:7070");
        _policy = HttpResiliencePolicies.CreateGrpcPolicyWrap(_logger);
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

            var response = await _policy.ExecuteAsync(() => client.UpdateLocationAsync(request).ResponseAsync);

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
            var response = await _policy.ExecuteAsync(() => client.GetCourierLocationAsync(request).ResponseAsync);

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

    public async Task<IReadOnlyList<(double Latitude, double Longitude, long TimestampMs, int Accuracy)>> GetCourierLocationHistoryAsync(
        Guid courierId,
        long fromTimestampMs = 0,
        int limit = 100)
    {
        try
        {
            var client = GetClient();
            var request = new GetLocationHistoryRequest
            {
                CourierId = courierId.ToString(),
                FromTimestampMs = fromTimestampMs,
                Limit = limit
            };

            var response = await _policy.ExecuteAsync(() => client.GetCourierLocationHistoryAsync(request).ResponseAsync);

            return response.Points
                .Select(p => (p.Latitude, p.Longitude, p.TimestampMs, p.Accuracy))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location history for courier {CourierId}", courierId);
            return Array.Empty<(double, double, long, int)>();
        }
    }
}
