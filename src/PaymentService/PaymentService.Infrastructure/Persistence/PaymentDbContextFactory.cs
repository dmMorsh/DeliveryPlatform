using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Persistence;

public sealed class PaymentDbContextFactory : IPaymentDbContextFactory
{
    public PaymentDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PaymentDbContext(options);
    }
}
