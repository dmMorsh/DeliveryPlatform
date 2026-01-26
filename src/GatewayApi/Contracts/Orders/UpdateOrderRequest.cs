namespace GatewayApi.Contracts.Orders;

/// <summary>
/// DTO для проксирования запросов обновления заказа
/// </summary>
public class UpdateOrderRequest
{
    public int? CourierId { get; set; }
    public int? Status { get; set; }
    public string? CourierNote { get; set; }
}