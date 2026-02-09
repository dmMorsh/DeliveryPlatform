using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Persistence;

public sealed class PaymentShardMapDbContextFactory : IPaymentShardMapDbContextFactory
{
    public PaymentShardMapDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PaymentShardMapDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PaymentShardMapDbContext(options);
    }
}
