namespace GatewayApi.Contracts.Orders;

/// <summary>
/// DTO for proxying order update requests
/// </summary>
public class UpdateOrderRequest
{
    public int? CourierId { get; set; }
    public int? Status { get; set; }
    public string? CourierNote { get; set; }
}
