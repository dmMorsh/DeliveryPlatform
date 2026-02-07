namespace PaymentService.Application.Models;

public record CreatePaymentModel(Guid OrderId, long Amount, string Currency);
