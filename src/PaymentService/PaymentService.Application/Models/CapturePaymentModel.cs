namespace PaymentService.Application.Models;

public record CapturePaymentModel(Guid OrderId, long? AmountCents);
