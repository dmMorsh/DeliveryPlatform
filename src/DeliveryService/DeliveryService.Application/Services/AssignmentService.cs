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
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly ICourierActivityStore _activityStore;
    private readonly ILogger<AssignmentService> _logger;
    private readonly DeliveryAssignmentOptions _options;
    private readonly CourierAvailabilityOptions _availabilityOptions;

    public AssignmentService(
        ICourierDirectory courierDirectory,
        IDeliveryRepository deliveryRepository,
        ICourierActivityStore activityStore,
        IOptions<DeliveryAssignmentOptions> options,
        IOptions<CourierAvailabilityOptions> availabilityOptions,
        ILogger<AssignmentService> logger)
    {
        _courierDirectory = courierDirectory;
        _deliveryRepository = deliveryRepository;
        _activityStore = activityStore;
        _logger = logger;
        _options = options.Value;
        _availabilityOptions = availabilityOptions.Value ?? new CourierAvailabilityOptions();
    }

    public async Task<bool> OfferNextCourierAsync(Delivery delivery, CancellationToken ct = default)
    {
        if (delivery.CurrentOfferExpiresAt.HasValue && delivery.CurrentOfferExpiresAt > DateTime.UtcNow)
            return false;

        delivery.ExpireCurrentOffer();

        var candidates = await _courierDirectory.GetActiveCouriersAsync(ct);
        if (candidates.Count == 0)
            return false;

        var now = DateTime.UtcNow;
        var activeCourierIds = new HashSet<Guid>();
        foreach (var candidate in candidates)
        {
            if (await _activityStore.IsActiveAsync(candidate.Id, now, ct))
                activeCourierIds.Add(candidate.Id);
        }
        if (activeCourierIds.Count == 0)
            return false;

        var activeDeliveries = await _deliveryRepository.GetActiveDeliveriesByCourierIdsAsync(activeCourierIds.ToList(), ct);

        var tried = delivery.AssignmentAttempts
            .Select(a => a.CourierId)
            .ToHashSet();

        var ranked = candidates
            .Where(c => activeCourierIds.Contains(c.Id))
            .Where(c => !tried.Contains(c.Id))
            .Where(c => CanOfferToCourier(c.Id, activeDeliveries, now))
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

    private bool CanOfferToCourier(Guid courierId, List<Delivery> activeDeliveries, DateTime now)
    {
        var maxActive = Math.Max(_availabilityOptions.MaxActiveDeliveries, 1);
        var allowExtraMinutes = Math.Max(_availabilityOptions.AllowExtraWhenMinutesLeft, 0);

        var courierDeliveries = activeDeliveries.Where(d => d.CourierId == courierId).ToList();
        if (courierDeliveries.Count < maxActive)
            return true;

        if (allowExtraMinutes == 0)
            return false;

        var inDelivery = courierDeliveries
            .Where(d => d.Status == DeliveryStatus.InDelivery && d.EstimatedDeliveryAt.HasValue)
            .OrderBy(d => d.EstimatedDeliveryAt)
            .FirstOrDefault();
        if (inDelivery == null)
            return false;

        return inDelivery.EstimatedDeliveryAt.Value <= now.AddMinutes(allowExtraMinutes);
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
