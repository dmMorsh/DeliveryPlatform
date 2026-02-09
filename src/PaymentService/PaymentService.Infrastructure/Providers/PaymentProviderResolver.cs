using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Providers;

public class PaymentProviderResolver : IPaymentProviderResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;

    public PaymentProviderResolver(IEnumerable<IPaymentProvider> providers)
    {
        var map = new Dictionary<string, IPaymentProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            map[provider.Name] = provider;
            foreach (var alias in provider.Aliases)
                map[alias] = provider;
        }

        _providers = map;
    }

    public IPaymentProvider Get(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required", nameof(providerName));

        if (_providers.TryGetValue(providerName, out var provider))
            return provider;

        throw new InvalidOperationException($"Payment provider '{providerName}' is not registered");
    }
}
