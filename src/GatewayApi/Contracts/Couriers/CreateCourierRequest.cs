namespace GatewayApi.Contracts.Couriers;

/// <summary>
/// DTO для проксирования запросов создания курьера
/// </summary>
public class CreateCourierRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
}