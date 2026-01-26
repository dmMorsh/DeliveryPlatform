namespace GatewayApi.Contracts.Orders;

/// <summary>
/// Фильтр для получения заказов
/// </summary>
public class GetOrdersFilterRequest
{
    public int? ClientId { get; set; }
    public int? CourierId { get; set; }
    public int? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}