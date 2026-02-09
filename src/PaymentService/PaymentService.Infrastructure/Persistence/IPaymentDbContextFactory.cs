namespace PaymentService.Infrastructure.Persistence;

public interface IPaymentDbContextFactory
{
    PaymentDbContext Create(string connectionString);
}
