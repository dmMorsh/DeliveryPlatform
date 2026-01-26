using CourierService.Domain.Events;
using CourierService.Domain.SeedWork;

namespace CourierService.Domain.Aggregates;

public enum CourierStatus
{
    Offline = 0,
    Online = 1,
    OnDelivery = 2,
    Resting = 3
}

public class Courier : AggregateRoot
{
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public CourierStatus Status { get; private set; } = CourierStatus.Offline;
    public double? CurrentLatitude { get; private set; }
    public double? CurrentLongitude { get; private set; }
    public DateTime? LastLocationUpdate { get; private set; }
    public double Rating { get; private set; } = 5.0;
    public int CompletedDeliveries { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Courier() { }

    public void Deactivate()
    {
        IsActive = false;
    }

    // Factory for registering new courier
    public static Courier Register(string fullName, string phone, string email, string documentNumber)
    {
        var c = new Courier
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Phone = phone,
            Email = email,
            DocumentNumber = documentNumber,
            Status = CourierStatus.Offline,
            Rating = 5.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        c.AddDomainEvent(new CourierRegisteredDomainEvent { CourierId = c.Id });
        return c;
    }

    public void ChangeStatus(CourierStatus newStatus)
    {
        var prev = Status;
        if (prev == newStatus) return;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CourierStatusChangedDomainEvent { CourierId = Id, PreviousStatus = prev, NewStatus = newStatus });
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        LastLocationUpdate = DateTime.UtcNow;
        AddDomainEvent(new CourierLocationUpdatedDomainEvent { CourierId = Id, Latitude = latitude, Longitude = longitude });
    }

    public void UpdateRating(double rating)
    {
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CourierRatingUpdatedDomainEvent { CourierId = Id, Rating = rating });
    }
}
