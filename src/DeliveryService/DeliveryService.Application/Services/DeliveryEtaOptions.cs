namespace DeliveryService.Application.Services;

public sealed class DeliveryEtaOptions
{
    public bool Enabled { get; set; } = true;
    public double AverageSpeedKmh { get; set; } = 25;
    public int PickupBufferMinutes { get; set; } = 10;
    public int MinTravelMinutes { get; set; } = 5;
}
