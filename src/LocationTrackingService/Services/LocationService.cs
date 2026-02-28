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
    private static readonly TimeSpan LocationTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan HistoryTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan HistorySampleInterval = TimeSpan.FromMinutes(1);
    private const int HistoryMaxItems = 2000;
    private const string AppendHistoryScript = """
        local last = redis.call('GET', KEYS[2])
        local now = tonumber(ARGV[1])
        local minInterval = tonumber(ARGV[2])
        if last and (now - tonumber(last)) < minInterval then
            return 0
        end
        redis.call('SET', KEYS[2], ARGV[1], 'PX', ARGV[3])
        redis.call('LPUSH', KEYS[1], ARGV[4])
        redis.call('LTRIM', KEYS[1], 0, tonumber(ARGV[5]) - 1)
        redis.call('PEXPIRE', KEYS[1], tonumber(ARGV[3]))
        return 1
        """;

    private readonly ILogger<LocationService> _logger;
    private readonly IDatabase _db;

    public LocationService(ILogger<LocationService> logger, IConnectionMultiplexer mux)
    {
        _logger = logger;
        _db = mux.GetDatabase();
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
        var nowMs = payload.UpdatedAt.ToUnixTimeMilliseconds();
        await _db.ScriptEvaluateAsync(
            AppendHistoryScript,
            new RedisKey[] { historyKey, historyTsKey },
            new RedisValue[]
            {
                nowMs,
                (long)HistorySampleInterval.TotalMilliseconds,
                (long)HistoryTtl.TotalMilliseconds,
                json,
                HistoryMaxItems
            });
    }

    private static string GetLocationKey(Guid courierId) => $"courier:{courierId}:location";
    private static string GetHistoryKey(Guid courierId) => $"courier:{courierId}:history";
    private static string GetHistoryTsKey(Guid courierId) => $"courier:{courierId}:history:last_ts";
}
