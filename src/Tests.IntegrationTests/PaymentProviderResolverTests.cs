using FluentAssertions;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Infrastructure.Providers;

namespace Tests.IntegrationTests;

public class PaymentProviderResolverTests
{
    private sealed class FakeProvider : IPaymentProvider
    {
        public FakeProvider(string name, params string[] aliases)
        {
            Name = name;
            Aliases = aliases;
        }

        public string Name { get; }
        public IReadOnlyCollection<string> Aliases { get; }

        public Task<StartPaymentResult> StartPayment(StartPaymentRequest request, CancellationToken ct)
            => Task.FromResult(new StartPaymentResult("ext", "https://pay"));

        public Task CapturePayment(string externalPaymentId, long? amountCents, string currency, CancellationToken ct)
            => Task.CompletedTask;

        public Task CancelPayment(string externalPaymentId, CancellationToken ct)
            => Task.CompletedTask;

        public Task RefundPayment(string externalPaymentId, long amountCents, string currency, CancellationToken ct)
            => Task.CompletedTask;

        public Task<PaymentProviderStatus> CheckStatus(string externalPaymentId, CancellationToken ct)
            => Task.FromResult(PaymentProviderStatus.Pending);
    }

    [Fact]
    public void Get_ShouldResolveByName()
    {
        var provider = new FakeProvider("sberbank", "sber");
        var resolver = new PaymentProviderResolver(new[] { provider });

        var resolved = resolver.Get("sberbank");

        resolved.Should().BeSameAs(provider);
    }

    [Fact]
    public void Get_ShouldResolveByAlias_CaseInsensitive()
    {
        var provider = new FakeProvider("yoomoney", "ym");
        var resolver = new PaymentProviderResolver(new[] { provider });

        var resolved = resolver.Get("YM");

        resolved.Should().BeSameAs(provider);
    }

    [Fact]
    public void Get_ShouldThrowWhenUnknown()
    {
        var provider = new FakeProvider("sberbank", "sber");
        var resolver = new PaymentProviderResolver(new[] { provider });

        var action = () => resolver.Get("unknown");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Payment provider 'unknown' is not registered");
    }
}
