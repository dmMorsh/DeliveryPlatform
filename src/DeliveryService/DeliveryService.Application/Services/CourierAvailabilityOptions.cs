namespace DeliveryService.Application.Services;

public sealed class CourierAvailabilityOptions
{
    public int MaxActiveDeliveries { get; set; } = 1;
    public int AllowExtraWhenMinutesLeft { get; set; } = 10;
}
