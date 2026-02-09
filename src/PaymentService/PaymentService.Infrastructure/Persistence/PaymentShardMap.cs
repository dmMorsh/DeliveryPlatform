namespace PaymentService.Infrastructure.Persistence;

public sealed class PaymentShardMap
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string ExternalPaymentId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
