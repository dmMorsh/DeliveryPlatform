namespace PaymentService.Application.Models;

public enum PaymentProviderStatus
{
    Pending,
    Authorized,
    Succeeded,
    Failed,
    Cancelled,
    Refunded
}
