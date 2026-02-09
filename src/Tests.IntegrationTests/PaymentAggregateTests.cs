using FluentAssertions;
using PaymentService.Domain.Aggregates;
using PaymentService.Domain.SeedWork;

namespace Tests.IntegrationTests;

public class PaymentAggregateTests
{
    [Fact]
    public void Create_ShouldInitializeDefaults()
    {
        var orderId = Guid.NewGuid();

        var payment = Payment.Create(orderId, 1500, "RUB");

        payment.Id.Should().NotBe(Guid.Empty);
        payment.OrderId.Should().Be(orderId);
        payment.AmountCents.Should().Be(1500);
        payment.Currency.Should().Be("RUB");
        payment.Status.Should().Be(PaymentStatus.Created);
    }

    [Fact]
    public void Start_FromCreated_ShouldMoveToPending()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");

        payment.Start("sberbank", "ext-1", "https://pay");

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Provider.Should().Be("sberbank");
        payment.ExternalPaymentId.Should().Be("ext-1");
        payment.PaymentUrl.Should().Be("https://pay");
    }

    [Fact]
    public void Start_FromNonCreated_ShouldThrow()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");

        var action = () => payment.Start("sberbank", "ext-2", "https://pay");

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAuthorized_FromPending_ShouldUpdate()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");

        payment.MarkAuthorized("auth-1");

        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.ExternalPaymentId.Should().Be("auth-1");
    }

    [Fact]
    public void MarkCaptured_FromPending_ShouldUpdate()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");

        payment.MarkCaptured("cap-1");

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.ExternalPaymentId.Should().Be("cap-1");
    }

    [Fact]
    public void MarkCaptured_FromCancelled_ShouldNotChange()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");
        payment.MarkCancelled();

        payment.MarkCaptured("cap-1");

        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void MarkCancelled_FromPending_ShouldUpdate()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");

        payment.MarkCancelled();

        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void MarkFailed_FromPending_ShouldUpdate()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");

        payment.MarkFailed("error");

        payment.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkRefunded_FromCaptured_ShouldUpdate()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");
        payment.MarkCaptured("cap-1");

        payment.MarkRefunded();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkFailed_FromCaptured_ShouldNotChange()
    {
        var payment = Payment.Create(Guid.NewGuid(), 1000, "RUB");
        payment.Start("sberbank", "ext-1", "https://pay");
        payment.MarkCaptured("cap-1");

        payment.MarkFailed("error");

        payment.Status.Should().Be(PaymentStatus.Captured);
    }
}
