namespace PaymentService.Infrastructure.Jobs;

public sealed class PaymentStatusCheckOptions
{
    public int[] DelaysSeconds { get; set; } = new[] { 30, 120, 300, 600, 1800 };
}
