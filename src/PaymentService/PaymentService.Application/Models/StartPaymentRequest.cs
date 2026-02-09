namespace PaymentService.Application.Models;

public record StartPaymentRequest(
    Guid PaymentId,
    Guid OrderId,
    long AmountCents,
    string Currency,
    string Description,
    bool Capture);
