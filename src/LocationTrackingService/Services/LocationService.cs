using System.Text.Json;
using StackExchange.Redis;

namespace LocationTrackingService.Services;

/// <summary>
/// Интерфейс для работы с локациями курьеров
/// </summary>
public interface ILocationService
{
    Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy, DateTimeOffset timestamp);
    Task<CourierLocationDto?> GetCourierLocationAsync(Guid courierId);
    Task<IReadOnlyList<CourierLocationDto>> GetCourierLocationHistoryAsync(Guid courierId, DateTimeOffset? from, int limit);
}

/// <summary>
/// DTO для представления локации курьера
/// </summary>
public class CourierLocationDto
{
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Accuracy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Реализация сервиса работы с локациями курьеров
/// </summary>
public class LocationService : ILocationService
{
    private const string LocationChannel = "courier.location.updated";
    private static readonly TimeSpan LocationTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan HistoryTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan HistorySampleInterval = TimeSpan.FromMinutes(1);
    private const int HistoryMaxItems = 2000;

    private readonly ILogger<LocationService> _logger;
    private readonly IDatabase _db;
    private readonly ISubscriber _subscriber;

    public LocationService(ILogger<LocationService> logger, IConnectionMultiplexer mux)
    {
        _logger = logger;
        _db = mux.GetDatabase();
        _subscriber = mux.GetSubscriber();
    }

    public async Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy, DateTimeOffset timestamp)
    {
        try
        {
            var payload = new CourierLocationDto
            {
                CourierId = courierId,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                UpdatedAt = timestamp
            };

            var json = JsonSerializer.Serialize(payload);
            var locationKey = GetLocationKey(courierId);
            await _db.StringSetAsync(locationKey, json, LocationTtl);

            await TryAppendHistoryAsync(courierId, payload, json);

            await _subscriber.PublishAsync(LocationChannel, json);

            _logger.LogInformation(
                "Location updated for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, latitude, longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for courier {CourierId}", courierId);
            throw;
        }
    }

    public async Task<CourierLocationDto?> GetCourierLocationAsync(Guid courierId)
    {
        try
        {
            var locationKey = GetLocationKey(courierId);
            var value = await _db.StringGetAsync(locationKey);
            if (value.IsNullOrEmpty)
            {
                _logger.LogWarning("Location not found for courier {CourierId}", courierId);
                return null;
            }
// TODO check value
            var location = JsonSerializer.Deserialize<CourierLocationDto>(value.ToString());
            if (location == null)
                return null;

            _logger.LogInformation(
                "Retrieved location for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, location.Latitude, location.Longitude);
            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for courier {CourierId}", courierId);
            throw;
        }
    }

    public async Task<IReadOnlyList<CourierLocationDto>> GetCourierLocationHistoryAsync(Guid courierId, DateTimeOffset? from, int limit)
    {
        if (limit <= 0)
            limit = 100;

        var historyKey = GetHistoryKey(courierId);
        var start = 0;
        var stop = limit - 1;

        var items = await _db.ListRangeAsync(historyKey, start, stop);
        var result = new List<CourierLocationDto>();
        foreach (var item in items)
        {// TODO check item
            var location = JsonSerializer.Deserialize<CourierLocationDto>(item.ToString());
            if (location == null)
                continue;

            if (from.HasValue && location.UpdatedAt < from.Value)
                continue;

            result.Add(location);
        }

        return result;
    }

    private async Task TryAppendHistoryAsync(Guid courierId, CourierLocationDto payload, string json)
    {
        var historyKey = GetHistoryKey(courierId);
        var historyTsKey = GetHistoryTsKey(courierId);
        var lastTsValue = await _db.StringGetAsync(historyTsKey);
        // TODO check val
        if (lastTsValue.HasValue && long.TryParse(lastTsValue.ToString(), out var lastTs))
        {
            var last = DateTimeOffset.FromUnixTimeMilliseconds(lastTs);
            if (payload.UpdatedAt - last < HistorySampleInterval)
                return;
        }

        await _db.StringSetAsync(historyTsKey, payload.UpdatedAt.ToUnixTimeMilliseconds(), HistoryTtl);
        await _db.ListLeftPushAsync(historyKey, json);
        await _db.ListTrimAsync(historyKey, 0, HistoryMaxItems - 1);
        await _db.KeyExpireAsync(historyKey, HistoryTtl);
    }

    private static string GetLocationKey(Guid courierId) => $"courier:{courierId}:location";
    private static string GetHistoryKey(Guid courierId) => $"courier:{courierId}:history";
    private static string GetHistoryTsKey(Guid courierId) => $"courier:{courierId}:history:last_ts";
}
