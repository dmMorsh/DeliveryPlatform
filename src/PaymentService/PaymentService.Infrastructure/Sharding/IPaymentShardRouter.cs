namespace PaymentService.Infrastructure.Sharding;

public interface IPaymentShardRouter
{
    string GetConnectionString(Guid orderId);
    IReadOnlyList<string> GetAllConnectionStrings();
}
