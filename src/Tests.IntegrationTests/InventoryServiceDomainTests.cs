using FluentAssertions;
using InventoryService.Domain.Aggregates;
using InventoryService.Domain.SeedWork;

namespace Tests.IntegrationTests;

public class InventoryServiceDomainTests
{
    [Fact]
    public void CreateStockItem_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 100;

        // Act
        var stock = new StockItem(productId, quantity);

        // Assert
        stock.Id.Should().Be(productId);
        stock.TotalQuantity.Should().Be(quantity);
        stock.ReservedQuantity.Should().Be(0);
        stock.AvailableQuantity.Should().Be(quantity);
    }

    [Fact]
    public void CreateStockItem_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange & Act
        var action = () => new StockItem(Guid.NewGuid(), -10);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Initial quantity cannot be negative");
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_ShouldIncrease()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        stock.AddStock(50);

        // Assert
        stock.TotalQuantity.Should().Be(150);
        stock.AvailableQuantity.Should().Be(150);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ShouldThrow()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var action = () => stock.AddStock(0);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Quantity must be positive");
    }

    [Fact]
    public void AddStock_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var action = () => stock.AddStock(-10);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Quantity must be positive");
    }

    [Fact]
    public void CanReserve_WithSufficientStock_ShouldReturnNull()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var error = stock.CanReserve(50);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void CanReserve_WithInsufficientStock_ShouldReturnError()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var error = stock.CanReserve(150);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Not enough stock");
    }

    [Fact]
    public void CanReserve_WithZeroQuantity_ShouldReturnError()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var error = stock.CanReserve(0);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Quantity must be positive");
    }

    [Fact]
    public void Reserve_WithValidQuantity_ShouldUpdateReservedQuantity()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        var orderId = Guid.NewGuid();

        // Act
        stock.Reserve(50, orderId);

        // Assert
        stock.ReservedQuantity.Should().Be(50);
        stock.AvailableQuantity.Should().Be(50);
        stock.TotalQuantity.Should().Be(100);
        stock.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Reserve_WithInsufficientStock_ShouldThrow()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        var action = () => stock.Reserve(150, Guid.NewGuid());

        // Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_MultipleReservations_ShouldAccumulate()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);

        // Act
        stock.Reserve(30, Guid.NewGuid());
        stock.Reserve(20, Guid.NewGuid());
        stock.Reserve(25, Guid.NewGuid());

        // Assert
        stock.ReservedQuantity.Should().Be(75);
        stock.AvailableQuantity.Should().Be(25);
    }

    [Fact]
    public void CanRelease_WithValidReservedQuantity_ShouldReturnNull()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(50, Guid.NewGuid());

        // Act
        var error = stock.CanRelease(30);

        // Assert
        error.Should().BeNull();
    }

    [Fact]
    public void CanRelease_WithMoreThanReserved_ShouldReturnError()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(50, Guid.NewGuid());

        // Act
        var error = stock.CanRelease(70);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Cannot release more than reserved");
    }

    [Fact]
    public void Release_WithValidQuantity_ShouldDecreaseReservedQuantity()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        var orderId = Guid.NewGuid();
        stock.Reserve(50, orderId);

        // Act
        stock.Release(30, orderId);

        // Assert
        stock.ReservedQuantity.Should().Be(20);
        stock.AvailableQuantity.Should().Be(80);
    }

    [Fact]
    public void Release_WithMoreThanReserved_ShouldThrow()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(50, Guid.NewGuid());

        // Act
        var action = () => stock.Release(70, Guid.NewGuid());

        // Assert
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void CommitReservation_ShouldDecreaseReservedAndTotal()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(50, Guid.NewGuid());

        // Act
        stock.CommitReservation(30);

        // Assert
        stock.ReservedQuantity.Should().Be(20);
        stock.TotalQuantity.Should().Be(70);
        stock.AvailableQuantity.Should().Be(50);
    }

    [Fact]
    public void SetTotalQuantity_WithValidValue_ShouldUpdate()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(30, Guid.NewGuid());

        // Act
        stock.SetTotalQuantity(150);

        // Assert
        stock.TotalQuantity.Should().Be(150);
        stock.AvailableQuantity.Should().Be(120); // 150 - 30 reserved
    }

    [Fact]
    public void SetTotalQuantity_BelowReserved_ShouldThrow()
    {
        // Arrange
        var stock = new StockItem(Guid.NewGuid(), 100);
        stock.Reserve(50, Guid.NewGuid());

        // Act
        var action = () => stock.SetTotalQuantity(30);

        // Assert
        action.Should().Throw<DomainException>()
            .WithMessage("Quantity must be more than or equal to ReservedQuantity");
    }
}
