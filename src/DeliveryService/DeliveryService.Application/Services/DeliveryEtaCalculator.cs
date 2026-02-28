using Microsoft.Extensions.Options;

namespace DeliveryService.Application.Services;

public sealed class DeliveryEtaCalculator : IDeliveryEtaCalculator
{
    private readonly DeliveryEtaOptions _options;

    public DeliveryEtaCalculator(IOptions<DeliveryEtaOptions> options)
    {
        _options = options.Value ?? new DeliveryEtaOptions();
    }

    public DeliveryEtaResult? Calculate(double fromLat, double fromLon, double toLat, double toLon)
    {
        if (!_options.Enabled || _options.AverageSpeedKmh <= 0)
            return null;

        var distanceKm = HaversineKm(fromLat, fromLon, toLat, toLon);
        var travelMinutes = (int)Math.Ceiling(distanceKm / _options.AverageSpeedKmh * 60);
        if (travelMinutes < _options.MinTravelMinutes)
            travelMinutes = _options.MinTravelMinutes;

        var now = DateTime.UtcNow;
        var estimatedPickupAt = now.AddMinutes(_options.PickupBufferMinutes);
        var estimatedDeliveryAt = estimatedPickupAt.AddMinutes(travelMinutes);

        return new DeliveryEtaResult(distanceKm, travelMinutes, estimatedPickupAt, estimatedDeliveryAt);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return radius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

public interface IDeliveryEtaCalculator
{
    DeliveryEtaResult? Calculate(double fromLat, double fromLon, double toLat, double toLon);
}

public sealed record DeliveryEtaResult(double DistanceKm, int TravelMinutes, DateTime EstimatedPickupAt, DateTime EstimatedDeliveryAt);
