namespace PaymentService.Application.Models;

public record PaymentStatusView
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string? ExternalPaymentId { get; init; }
    public string? PaymentUrl { get; init; }
    public long AmountCents { get; init; }
    public string Currency { get; init; } = string.Empty;
}
