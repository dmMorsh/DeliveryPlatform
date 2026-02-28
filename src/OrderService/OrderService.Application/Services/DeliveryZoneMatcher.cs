using Microsoft.Extensions.Options;

namespace OrderService.Application.Services;

public sealed class DeliveryZoneMatcher : IDeliveryZoneMatcher
{
    private readonly DeliveryZoneOptions _options;

    public DeliveryZoneMatcher(IOptions<DeliveryZoneOptions> options)
    {
        _options = options.Value ?? new DeliveryZoneOptions();
    }

    public DeliveryZoneMatchResult? Match(double latitude, double longitude)
    {
        if (!_options.Enabled || _options.Zones.Count == 0)
            return null;

        DeliveryZoneMatchResult? best = null;
        foreach (var zone in _options.Zones)
        {
            if (zone.RadiusKm <= 0)
                continue;

            var distanceKm = HaversineKm(latitude, longitude, zone.CenterLatitude, zone.CenterLongitude);
            if (distanceKm > zone.RadiusKm)
                continue;

            if (best == null || distanceKm < best.DistanceKm)
            {
                best = new DeliveryZoneMatchResult(zone, distanceKm);
            }
        }

        return best;
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

public interface IDeliveryZoneMatcher
{
    DeliveryZoneMatchResult? Match(double latitude, double longitude);
}

public sealed record DeliveryZoneMatchResult(DeliveryZoneDefinition Zone, double DistanceKm);
