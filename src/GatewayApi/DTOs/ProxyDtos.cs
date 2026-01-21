namespace GatewayApi.DTOs;

/// <summary>
/// DTO для проксирования запросов создания заказа
/// </summary>
public class CreateOrderProxyDto
{
    public int ClientId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public long CostCents { get; set; }
    public string? CourierNote { get; set; }
}

/// <summary>
/// DTO для проксирования запросов обновления заказа
/// </summary>
public class UpdateOrderProxyDto
{
    public int? CourierId { get; set; }
    public int? Status { get; set; }
    public string? CourierNote { get; set; }
}

/// <summary>
/// DTO для проксирования запросов создания курьера
/// </summary>
public class CreateCourierProxyDto
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO для проксирования запросов обновления курьера
/// </summary>
public class UpdateCourierProxyDto
{
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public double? Rating { get; set; }
}

/// <summary>
/// Обёртка для ошибок при проксировании
/// </summary>
public class ProxyErrorResponse
{
    public bool Success { get; set; } = false;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Фильтр для получения заказов
/// </summary>
public class GetOrdersFilterDto
{
    public int? ClientId { get; set; }
    public int? CourierId { get; set; }
    public int? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
