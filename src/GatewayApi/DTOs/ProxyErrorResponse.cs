namespace GatewayApi.DTOs;

/// <summary>
/// Обёртка для ошибок при проксировании
/// </summary>
public class ProxyErrorResponse
{
    public bool Success { get; set; } = false;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}