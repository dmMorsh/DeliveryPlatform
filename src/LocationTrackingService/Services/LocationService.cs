using System.Collections.Concurrent;

namespace LocationTrackingService.Services;

/// <summary>
/// Интерфейс для работы с локациями курьеров
/// </summary>
public interface ILocationService
{
    Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy, DateTimeOffset timestamp);
    Task<CourierLocationDto?> GetCourierLocationAsync(Guid courierId);
}

/// <summary>
/// DTO для представления локации курьера
/// </summary>
public class CourierLocationDto
{
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Реализация сервиса работы с локациями курьеров
/// </summary>
public class LocationService : ILocationService
{
    private readonly ILogger<LocationService> _logger;
    private static readonly ConcurrentDictionary<Guid, CourierLocationDto> LocationStore = new();

    public LocationService(ILogger<LocationService> logger)
    {
        _logger = logger;
    }

    public async Task UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy, DateTimeOffset timestamp)
    {
        try
        {
            var location = new CourierLocationDto
            {
                CourierId = courierId,
                Latitude = latitude,
                Longitude = longitude,
                UpdatedAt = timestamp
            };

            LocationStore[courierId] = location;
            
            _logger.LogInformation(
                "Location updated for courier {CourierId}: ({Latitude}, {Longitude})",
                courierId, latitude, longitude);

            await Task.CompletedTask;
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
            if (LocationStore.TryGetValue(courierId, out var location))
            {
                _logger.LogInformation(
                    "Retrieved location for courier {CourierId}: ({Latitude}, {Longitude})",
                    courierId, location.Latitude, location.Longitude);
                return await Task.FromResult(location);
            }

            _logger.LogWarning("Location not found for courier {CourierId}", courierId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for courier {CourierId}", courierId);
            throw;
        }
    }

    public void ClearLocationStore()
    {
        LocationStore.Clear();
    }

    /// <summary>
    /// Static method for testing to clear the location store
    /// </summary>
    public static void ClearSharedLocationStore()
    {
        LocationStore.Clear();
    }
}
