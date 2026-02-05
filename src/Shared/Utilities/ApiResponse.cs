namespace Shared.Utilities;

/// <summary>
/// Результат операции для стандартизованного ответа API
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> ErrorResponse(params string[] errors)
        => new() { Success = false, Errors = errors.ToList() };
    
    public static ApiResponse<T> ErrorResponse(T data, string? message = null)
        => new() { Success = false, Data = data, Message = message };
}

/// <summary>
/// Результат операции без возвращаемого значения
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse SuccessResponse(string? message = null)
        => new() { Success = true, Message = message };

    public static ApiResponse ErrorResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// Информация об ошибке для логирования
/// </summary>
public class ErrorDetail
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
