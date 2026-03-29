namespace Shared.Utilities;

using Contracts.Events;

/// <summary>
/// Utility for serializing events to JSON
/// </summary>
public static class EventSerializer
{
    /// <summary>
    /// Serialize integration event contract to JSON
    /// </summary>
    public static string SerializeEvent(IntegrationEvent @event)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(@event, @event.GetType());
        return json;
    }

    /// <summary>
    /// Deserialize JSON into the integration event contract by type
    /// </summary>
    public static IntegrationEvent? DeserializeEvent(string json, string eventType)
    {
        var type = eventType switch
        {
            "order.created" => typeof(OrderCreatedEvent),
            "order.assigned" => typeof(OrderAssignedEvent),
            "order.status.changed" => typeof(OrderStatusChangedEvent),
            "order.delivered" => typeof(OrderDeliveredEvent),
            "payment.created" => typeof(PaymentCreatedEvent),
            "payment.authorized" => typeof(PaymentAuthorizedEvent),
            "payment.captured" => typeof(PaymentCapturedEvent),
            "payment.failed" => typeof(PaymentFailedEvent),
            "payment.cancelled" => typeof(PaymentCancelledEvent),
            "payment.refunded" => typeof(PaymentRefundedEvent),
            "courier.registered" => typeof(CourierRegisteredEvent),
            "courier.status.changed" => typeof(CourierStatusChangedEvent),
            "courier.rating.updated" => typeof(CourierRatingUpdatedEvent),
            _ => null
        };

        if (type == null)
            return null;

        return (IntegrationEvent?)System.Text.Json.JsonSerializer.Deserialize(json, type);
    }
}

/// <summary>
/// Utility for coordinate calculations
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Distance between two points in meters (Haversine formula)
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
/// Utility for generating unique order numbers
/// </summary>
public static class OrderNumberGenerator
{
    /// <summary>
    /// Generate an order number in ORD-YYYYMMDD-XXXXXXXX format (X = random digits)
    /// </summary>
    public static string GenerateOrderNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10000000, 100000000);
        return $"ORD-{date}-{random}";
    }
}
