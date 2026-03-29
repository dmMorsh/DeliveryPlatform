namespace GatewayApi.DTOs;

/// <summary>
/// Wrapper for proxying errors
/// </summary>
public class ProxyErrorResponse
{
    public bool Success { get; set; } = false;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
