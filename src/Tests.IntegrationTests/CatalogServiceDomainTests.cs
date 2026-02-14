using FluentAssertions;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.ValueObjects;

namespace Tests.IntegrationTests;

public class CatalogServiceDomainTests
{
    [Fact]
    public void CreateProduct_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "Test Product";
        var description = "Test Description";
        var price = new Money(1000, "USD");
        var weight = new Weight(500);

        // Act
        var product = new Product(name, description, price, weight);

        // Assert
        product.Id.Should().NotBe(Guid.Empty);
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.PriceCents.Should().Be(price);
        product.WeightGrams.Should().Be(weight);
        product.IsActive.Should().BeTrue();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangePrice_WithNewPrice_ShouldUpdateAndRaiseDomainEvent()
    {
        // Arrange
        var product = new Product("Test", null, new Money(1000, "USD"), new Weight(500));
        var newPrice = new Money(2000, "USD");

        // Act
        product.ChangePrice(newPrice);

        // Assert
        product.PriceCents.Should().Be(newPrice);
        product.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ChangePrice_WithSamePrice_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var price = new Money(1000, "USD");
        var product = new Product("Test", null, price, new Weight(500));

        // Act
        product.ChangePrice(price);

        // Assert
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeDescription_WithNewDescription_ShouldUpdate()
    {
        // Arrange
        var product = new Product("Test", "Old Description", new Money(1000, "USD"), new Weight(500));
        var newDescription = "New Description";

        // Act
        product.ChangeDescription(newDescription);

        // Assert
        product.Description.Should().Be(newDescription);
    }

    [Fact]
    public void ChangeWeight_WithNewWeight_ShouldUpdate()
    {
        // Arrange
        var product = new Product("Test", null, new Money(1000, "USD"), new Weight(500));
        var newWeight = new Weight(1000);

        // Act
        product.ChangeWeight(newWeight);

        // Assert
        product.WeightGrams.Should().Be(newWeight);
    }

    [Fact]
    public void Money_WithValidCurrency_ShouldCreate()
    {
        // Arrange & Act
        var money = new Money(1000, "USD");

        // Assert
        money.AmountCents.Should().Be(1000);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_WithMultipleCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(1000, "USD");
        var money2 = new Money(1000, "EUR");

        // Act & Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void Money_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(1000, "USD");
        var money2 = new Money(1000, "USD");

        // Act & Assert
        money1.Should().Be(money2);
    }
}
