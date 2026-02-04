using OrderService.Domain.Entities;

namespace OrderService.Application.Models;

public record UpdateOrderItemsModel(
    OrderItemStatus Status,
    IReadOnlyCollection<UpdateOrderItemModel> Items,
    string? Description = null);
    
public record UpdateOrderItemModel(Guid ProductId, int Quantity, string? Description);