namespace PaymentService.Api.Contracts;

public record CreatePaymentRequest(Guid OrderId, long Amount, string Currency);
