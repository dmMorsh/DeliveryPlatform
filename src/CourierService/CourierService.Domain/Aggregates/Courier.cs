using CourierService.Domain.Events;

namespace CourierService.Domain.Aggregates;

public enum CourierStatus
{
    Offline = 0,
    Online = 1,
    OnDelivery = 2,
    Resting = 3
}

public class Courier
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public CourierStatus Status { get; set; } = CourierStatus.Offline;
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public double Rating { get; set; } = 5.0;
    public int CompletedDeliveries { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Domain events
    private readonly List<CourierDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<CourierDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(CourierDomainEvent evt) => _domainEvents.Add(evt);
    public void ClearDomainEvents() => _domainEvents.Clear();

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
