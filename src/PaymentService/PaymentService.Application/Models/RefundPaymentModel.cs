namespace PaymentService.Application.Models;

public record RefundPaymentModel(Guid OrderId, long AmountCents);
