namespace PaymentService.Application.Interfaces;

public interface IPaymentProviderResolver
{
    IPaymentProvider Get(string providerName);
}
