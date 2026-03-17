using Grpc.Net.Client;
using LocationTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Shared.Services;

public class LocationTrackingClient : ILocationTrackingClient
{
    private readonly ILogger<LocationTrackingClient> _logger;
    private readonly string _serviceUrl;
    private readonly IAsyncPolicy _policy;
    private readonly int _timeoutSeconds;
    private LocationTrackingService.LocationTrackingServiceClient? _client;

    public LocationTrackingClient(IConfiguration config, IHostEnvironment env, ILogger<LocationTrackingClient> logger)
    {
        _logger = logger;
        _serviceUrl = ConfigurationGuard.GetRequired(config, env, "gRPC:LocationTrackingService:Url", "https://localhost:7070");
        _timeoutSeconds = int.TryParse(config["gRPC:LocationTrackingService:TimeoutSeconds"], out var timeout) ? timeout : 10;
        _policy = HttpResiliencePolicies.CreateGrpcPolicyWrap(_logger, _timeoutSeconds);
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

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            var response = await _policy.ExecuteAsync(() => client.UpdateLocationAsync(request, cancellationToken: cts.Token).ResponseAsync);

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            var response = await _policy.ExecuteAsync(() => client.GetCourierLocationAsync(request, cancellationToken: cts.Token).ResponseAsync);

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

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            var response = await _policy.ExecuteAsync(() => client.GetCourierLocationHistoryAsync(request, cancellationToken: cts.Token).ResponseAsync);

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
