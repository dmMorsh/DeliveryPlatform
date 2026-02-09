namespace PaymentService.Infrastructure.Persistence;

public interface IPaymentShardMapDbContextFactory
{
    PaymentShardMapDbContext Create(string connectionString);
}
