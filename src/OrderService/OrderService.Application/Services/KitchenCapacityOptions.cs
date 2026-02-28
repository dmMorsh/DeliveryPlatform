namespace OrderService.Application.Services;

public sealed class KitchenCapacityOptions
{
    public bool Enabled { get; set; } = true;
    public int SlotMinutes { get; set; } = 15;
    public int MaxOrdersPerSlot { get; set; } = 20;
    public int PreparationMinutes { get; set; } = 30;
    public int LookaheadSlots { get; set; } = 8;
    public int PauseMinutesOnOverload { get; set; } = 10;
}
