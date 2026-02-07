namespace PaymentService.Application.Models;

public record PaymentView(Guid Id, long Amount, DateTime CreatedAt);
