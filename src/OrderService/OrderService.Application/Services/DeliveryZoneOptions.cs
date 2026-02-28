namespace OrderService.Application.Services;

public sealed class DeliveryZoneOptions
{
    public bool Enabled { get; set; } = false;
    public bool UseToCoordinates { get; set; } = true;
    public List<DeliveryZoneDefinition> Zones { get; set; } = new();
}

public sealed class DeliveryZoneDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double RadiusKm { get; set; }
    public int PickupSlaMinutes { get; set; } = 60;
    public int TransitSlaMinutes { get; set; } = 180;
    public double DeliveryFeeMultiplier { get; set; } = 1.0;
}
