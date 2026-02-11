namespace WebApp.Models;

public class DeliverySimulatorViewModel
{
    public CourierRegisterModel Register { get; set; } = new();
    public CourierUpdateModel CourierUpdate { get; set; } = new();
    public DeliveryActionModel DeliveryAction { get; set; } = new();
    public LocationUpdateModel LocationUpdate { get; set; } = new();
    public DeliveryLookupModel DeliveryLookup { get; set; } = new();
}

public class CourierRegisterModel
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
}

public class CourierUpdateModel
{
    public Guid CourierId { get; set; }
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public bool? IsActive { get; set; }
}

public class DeliveryActionModel
{
    public Guid? DeliveryId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid CourierId { get; set; }
    public string? Reason { get; set; }
    public string? Signature { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public string? VerificationCode { get; set; }
}

public class LocationUpdateModel
{
    public Guid CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Accuracy { get; set; } = 5;
}

public class DeliveryLookupModel
{
    public Guid? OrderId { get; set; }
    public Guid? DeliveryId { get; set; }
}
