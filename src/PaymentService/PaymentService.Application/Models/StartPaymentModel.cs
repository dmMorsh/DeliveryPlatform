namespace PaymentService.Application.Models;

public record StartPaymentModel(Guid OrderId, string Provider, bool Capture = true);
