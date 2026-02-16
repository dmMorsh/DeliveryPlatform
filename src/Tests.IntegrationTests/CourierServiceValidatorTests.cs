using CourierService.Application.Commands.RegisterCourier;
using CourierService.Application.Commands.UpdateCourierStatus;
using FluentAssertions;

namespace Tests.IntegrationTests;

public class CourierServiceValidatorTests
{
    private readonly RegisterCourierCommandValidator _registerValidator = new();
    private readonly UpdateCourierStatusCommandValidator _updateValidator = new();

    [Fact]
    public void RegisterCourierCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new RegisterCourierCommand
        (
            "John Doe",
            "+1234567890",
            "john@example.com",
            "12345"
        );

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterCourierCommandValidator_WithEmptyName_ShouldFail()
    {
        // Arrange
        var command = new RegisterCourierCommand(
            "",
            "+1234567890",
            "john@example.com",
            "12345"
        );

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.FullName");
    }

    [Fact]
    public void RegisterCourierCommandValidator_WithLongName_ShouldFail()
    {
        // Arrange
        var command = new RegisterCourierCommand(
            new string('a', 201),
            "+1234567890",
            "john@example.com",
            "12345"
        );

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.FullName");
    }

    [Fact]
    public void RegisterCourierCommandValidator_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var command = new RegisterCourierCommand(
            "John Doe",
            "+1234567890",
            "invalid-email",
            "12345"
        );

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.Email");
    }

    [Fact]
    public void RegisterCourierCommandValidator_WithEmptyPhone_ShouldFail()
    {
        // Arrange
        var command = new RegisterCourierCommand(
            "John Doe",
            "",
            "john@example.com",
            "12345"
        );

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.Phone");
    }

    [Fact]
    public void UpdateCourierStatusCommandValidator_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new UpdateCourierStatusCommand(
            Guid.NewGuid(),
            1, 
            null,
            null, 
            null
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateCourierStatusCommandValidator_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new UpdateCourierStatusCommand(
            Guid.Empty,
            1, 
            null,
            null, 
            null
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "CourierId");
    }

    [Fact]
    public void UpdateCourierStatusCommandValidator_WithNullStatus_ShouldFail()
    {
        // Arrange
        var command = new UpdateCourierStatusCommand(
            Guid.NewGuid(), 
            null, 
            null, 
            null, 
            null 
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.Status");
    }

    [Fact]
    public void UpdateCourierStatusCommandValidator_WithInvalidStatus_ShouldFail()
    {
        // Arrange
        var command = new UpdateCourierStatusCommand(
            Guid.NewGuid(),
            999, // Invalid status code
            null,
            null, 
            null
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.Status");
    }
}
