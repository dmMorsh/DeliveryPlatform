using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReleaseStock;

public class ReleaseStockCommand : IRequest<ApiResponse<Unit>>
{
    public Guid ProductId { get; }
    public int Quantity { get; }
    public Guid OrderId { get; }

    public ReleaseStockCommand(Guid productId, int quantity, Guid orderId)
    {
        ProductId = productId;
        Quantity = quantity;
        OrderId = orderId;
    }
}
