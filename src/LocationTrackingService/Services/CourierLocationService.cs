namespace LocationTrackingService.Services;

/// <summary>
/// Интерфейс для управления локациями курьеров
/// </summary>
public interface ICourierLocationService
{
    /// <summary>Обновить локацию курьера</summary>
    Task UpdateLocationAsync(Guid courierId, double latitude, double longitude);
    
    /// <summary>Получить текущую локацию курьера</summary>
    Task<CourierLocationData?> GetLocationAsync(Guid courierId);
    
    /// <summary>Получить все активные локации</summary>
    Task<List<CourierLocationData>> GetAllActiveLocationsAsync();
}

/// <summary>
/// Модель локации курьера
/// </summary>
public class CourierLocationData
{
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime LastUpdateAt { get; set; }
    public int Status { get; set; } // 0=offline, 1=online, 2=on_delivery, 3=resting
}

/// <summary>
/// In-memory хранилище локаций (в production использовать Redis)
/// </summary>
public class CourierLocationService : ICourierLocationService
{
    private readonly StackExchange.Redis.IDatabase _redis;
    private readonly ILogger<CourierLocationService> _logger;

    // Redis keys
    private const string ActiveSetKey = "couriers:active"; // sorted set courierGuid -> lastUpdate ticks

    public CourierLocationService(StackExchange.Redis.IDatabase redis, ILogger<CourierLocationService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task UpdateLocationAsync(Guid courierId, double latitude, double longitude)
    {
        var key = $"courier:{courierId}";
        var now = DateTime.UtcNow;

        var tran = _redis.CreateTransaction();
        _ = tran.HashSetAsync(key, new StackExchange.Redis.HashEntry[] {
            new StackExchange.Redis.HashEntry("latitude", latitude),
            new StackExchange.Redis.HashEntry("longitude", longitude),
            new StackExchange.Redis.HashEntry("last_update_ticks", now.Ticks),
            new StackExchange.Redis.HashEntry("status", 1)
        });
        _ = tran.SortedSetAddAsync(ActiveSetKey, courierId.ToString(), now.Ticks);
        await tran.ExecuteAsync();

        _logger.LogInformation("Courier {CourierId} location updated in Redis: ({Lat}, {Lon})", courierId, latitude, longitude);
    }

    public async Task<CourierLocationData?> GetLocationAsync(Guid courierId)
    {
        var key = $"courier:{courierId}";
        var entries = await _redis.HashGetAllAsync(key);
        if (entries == null || entries.Length == 0)
            return null;

        double lat = (double)entries.FirstOrDefault(e => e.Name == "latitude").Value;
        double lon = (double)entries.FirstOrDefault(e => e.Name == "longitude").Value;
        long ticks = (long)entries.FirstOrDefault(e => e.Name == "last_update_ticks").Value;
        int status = (int)entries.FirstOrDefault(e => e.Name == "status").Value;

        return new CourierLocationData
        {
            CourierId = courierId,
            Latitude = lat,
            Longitude = lon,
            LastUpdateAt = new DateTime(ticks, DateTimeKind.Utc),
            Status = status
        };
    }

    public async Task<List<CourierLocationData>> GetAllActiveLocationsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5).Ticks;
        var members = await _redis.SortedSetRangeByScoreAsync(ActiveSetKey, cutoff, double.PositiveInfinity);
        var result = new List<CourierLocationData>(members.Length);

        foreach (var member in members)
        {
            if (!Guid.TryParse(member, out var guid))
                continue;

            var loc = await GetLocationAsync(guid);
            if (loc != null)
                result.Add(loc);
        }

        return result;
    }
}
