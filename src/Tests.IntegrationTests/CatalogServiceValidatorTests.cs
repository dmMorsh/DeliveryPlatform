using CatalogService.Application.Commands.CreateProduct;
using CatalogService.Application.Commands.UpdateProduct;
using FluentAssertions;

namespace Tests.IntegrationTests;

public class CatalogServiceValidatorTests
{
    private readonly CreateProductCommandValidator _createValidator = new();
    private readonly UpdateProductCommandValidator _updateValidator = new();

    [Fact]
    public void CreateProductCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            9999,
            "USDT",
            1000
        );

        // Act
        var result = _createValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProductCommandValidator_WithEmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreateProductCommand(
            "",
            "Test Description",
            9999,
            "USD",
            1000
        );

        // Act
        var result = _createValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Name");
    }

    [Fact]
    public void CreateProductCommandValidator_WithLongName_ShouldFail()
    {
        // Arrange
        var longName = new string('a', 201);
        var command = new CreateProductCommand(
            longName,
            "Test Description",
            9999,
            "USDT",
            1000
        );

        // Act
        var result = _createValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Name");
    }

    [Fact]
    public void CreateProductCommandValidator_WithInvalidCurrency_ShouldFail()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            9999,
            "VERYLONGCURRENCY",  // More than 6 chars
            1000
        );

        // Act
        var result = _createValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Currency");
    }

    [Fact]
    public void CreateProductCommandValidator_WithZeroPrice_ShouldFail()
    {
        // Arrange
        var command = new CreateProductCommand(
            "Test Product",
            "Test Description",
            0,
            "USDT",
            1000
        );

        // Act
        var result = _createValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "PriceCents");
    }

    [Fact]
    public void UpdateProductCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.NewGuid(),
            "Updated Product",
            "Updated Description",
            9999,
            "USDT",
            null
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateProductCommandValidator_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new UpdateProductCommand(
            Guid.Empty,
            "Updated Product",
            "Updated Description",
            9999,
            "USD",
            null
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "ProductId");
    }
}
