namespace PaymentService.Infrastructure.Jobs;

public sealed class PaymentStatusCheckOptions
{
    public int[] DelaysSeconds { get; set; } = [];
}
