namespace OrderService.Application.Models;

public record UpdateOrderItemsModel(
    ItemModeStatus Status,
    IReadOnlyCollection<UpdateOrderItemModel> Items,
    string? Description = null);

public record UpdateOrderItemModel(Guid ProductId, int Quantity, string? Description);

public enum ItemModeStatus
{
    Reserved,
    ReservationFailed,
}
