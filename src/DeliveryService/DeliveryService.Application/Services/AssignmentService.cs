using DeliveryService.Application.Interfaces;
using DeliveryService.Domain.Aggregates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeliveryService.Application.Services;

public class DeliveryAssignmentOptions
{
    public int OfferTtlSeconds { get; set; } = 30;
    public int MaxCandidates { get; set; } = 10;
}

public class AssignmentService : IAssignmentService
{
    private readonly ICourierDirectory _courierDirectory;
    private readonly ILogger<AssignmentService> _logger;
    private readonly DeliveryAssignmentOptions _options;

    public AssignmentService(
        ICourierDirectory courierDirectory,
        IOptions<DeliveryAssignmentOptions> options,
        ILogger<AssignmentService> logger)
    {
        _courierDirectory = courierDirectory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<bool> OfferNextCourierAsync(Delivery delivery, CancellationToken ct = default)
    {
        if (delivery.CurrentOfferExpiresAt.HasValue && delivery.CurrentOfferExpiresAt > DateTime.UtcNow)
            return false;

        delivery.ExpireCurrentOffer();

        var candidates = await _courierDirectory.GetActiveCouriersAsync(ct);
        if (candidates.Count == 0)
            return false;

        var tried = delivery.AssignmentAttempts
            .Select(a => a.CourierId)
            .ToHashSet();

        var ranked = candidates
            .Where(c => !tried.Contains(c.Id))
            .Select(c => new
            {
                Courier = c,
                Distance = GetDistance(delivery.FromLatitude, delivery.FromLongitude, c.Latitude, c.Longitude),
                c.Rating
            })
            .OrderBy(x => x.Distance)
            .ThenByDescending(x => x.Rating)
            .Take(_options.MaxCandidates)
            .ToList();

        var best = ranked.FirstOrDefault();
        if (best == null)
            return false;

        var expiresAt = DateTime.UtcNow.AddSeconds(_options.OfferTtlSeconds);
        delivery.OfferToCourier(best.Courier.Id, expiresAt);

        _logger.LogInformation("Offered delivery {DeliveryId} to courier {CourierId}", delivery.Id, best.Courier.Id);
        return true;
    }

    private static double GetDistance(double lat1, double lon1, double? lat2, double? lon2)
    {
        if (!lat2.HasValue || !lon2.HasValue)
            return double.MaxValue;

        const double r = 6371; // km
        var dLat = DegreesToRadians(lat2.Value - lat1);
        var dLon = DegreesToRadians(lon2.Value - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2.Value)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180);
}
