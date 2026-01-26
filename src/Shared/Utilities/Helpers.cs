namespace Shared.Utilities;

using Contracts.Events;

/// <summary>
/// Утилита для сериализации событий в JSON
/// </summary>
public static class EventSerializer
{
    /// <summary>
    /// Сериализует интеграционный контракт события в JSON
    /// </summary>
    public static string SerializeEvent(IntegrationEvent @event)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType());
        return json;
    }

    /// <summary>
    /// Десериализует JSON в интеграционный контракт события по типу
    /// </summary>
    public static IntegrationEvent? DeserializeEvent(string json, string eventType)
    {
        var type = eventType switch
        {
            "order.created" => typeof(OrderCreatedEvent),
            "order.assigned" => typeof(OrderAssignedEvent),
            "order.status.changed" => typeof(OrderStatusChangedEvent),
            "order.delivered" => typeof(OrderDeliveredEvent),
            "courier.registered" => typeof(CourierRegisteredEvent),
            "courier.status.changed" => typeof(CourierStatusChangedEvent),
            "courier.location.updated" => typeof(CourierLocationUpdatedEvent),
            "courier.rating.updated" => typeof(CourierRatingUpdatedEvent),
            _ => null
        };

        if (type == null)
            return null;

        return (IntegrationEvent?)System.Text.Json.JsonSerializer.Deserialize(json, type);
    }
}

/// <summary>
/// Утилита для работы с координатами
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Расстояние между двумя точками в метрах (формула Haversine)
    /// </summary>
    public static double DistanceInMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMeters = 6371000;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180;
}

/// <summary>
/// Утилита для генерации уникальных номеров заказов
/// </summary>
public static class OrderNumberGenerator
{
    /// <summary>
    /// Генерирует номер заказа в формате ORD-YYYYMMDD-XXXXXXXX (где X - случайные символы)
    /// </summary>
    public static string GenerateOrderNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(10000000, 99999999);
        return $"ORD-{date}-{random}";
    }
}
