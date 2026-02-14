using FluentAssertions;
using InventoryService.Application.Commands.AddStock;
using InventoryService.Application.Commands.AdjustStock;
using InventoryService.Application.Commands.ReleaseStock;
using InventoryService.Application.Commands.ReserveStock;
using InventoryService.Application.Models;

namespace Tests.IntegrationTests;

public class InventoryServiceValidatorTests
{
    private readonly AddStockCommandValidator _addStockValidator = new();
    private readonly ReserveStockCommandValidator _reserveValidator = new();
    private readonly ReleaseStockCommandValidator _releaseValidator = new();
    private readonly AdjustStockCommandValidator _adjustValidator = new();

    [Fact]
    public void AddStockCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new AddStockCommand(new[]
        {
            new SimpleStockItemModel(Guid.NewGuid(), 100)
        });

        // Act
        var result = _addStockValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddStockCommandValidator_WithEmptyModels_ShouldFail()
    {
        // Arrange
        var command = new AddStockCommand(Array.Empty<SimpleStockItemModel>());

        // Act
        var result = _addStockValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void AddStockCommandValidator_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var command = new AddStockCommand(new[]
        {
            new SimpleStockItemModel(Guid.NewGuid(), 0)
        });

        // Act
        var result = _addStockValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Quantity"));
    }

    [Fact]
    public void ReserveStockCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            new[]
            {
                new SimpleStockItemModel(Guid.NewGuid(), 50)
            }
        );

        // Act
        var result = _reserveValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReserveStockCommandValidator_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new ReserveStockCommand(
            Guid.Empty,
            new[]
            {
                new SimpleStockItemModel(Guid.NewGuid(), 50)
            }
        );

        // Act
        var result = _reserveValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "OrderId");
    }

    [Fact]
    public void ReserveStockCommandValidator_WithMultipleItems_ShouldPass()
    {
        // Arrange
        var command = new ReserveStockCommand(
            Guid.NewGuid(),
            new[]
            {
                new SimpleStockItemModel(Guid.NewGuid(), 50),
                new SimpleStockItemModel(Guid.NewGuid(), 30)
            }
        );

        // Act
        var result = _reserveValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReleaseStockCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new ReleaseStockCommand(
            Guid.NewGuid(),
            new[]
            {
                new SimpleStockItemModel(Guid.NewGuid(), 30)
            }
        );

        // Act
        var result = _releaseValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReleaseStockCommandValidator_WithEmptyOrderId_ShouldFail()
    {
        // Arrange
        var command = new ReleaseStockCommand(
            Guid.Empty,
            new[]
            {
                new SimpleStockItemModel(Guid.NewGuid(), 30)
            }
        );

        // Act
        var result = _releaseValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "OrderId");
    }

    [Fact]
    public void AdjustStockCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new AdjustStockCommand(new[]
        {
            new SimpleStockItemModel(Guid.NewGuid(), 100)
        });

        // Act
        var result = _adjustValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AdjustStockCommandValidator_WithNegativeQuantity_ShouldFail()
    {
        // Arrange
        var command = new AdjustStockCommand(new[]
        {
            new SimpleStockItemModel(Guid.NewGuid(), -10)
        });

        // Act
        var result = _adjustValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Quantity"));
    }
}
