namespace GatewayApi.Contracts.Couriers;

/// <summary>
/// DTO для проксирования запросов обновления курьера
/// </summary>
public class UpdateCourierRequest
{
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public double? Rating { get; set; }
}