using CourierService.Application.Commands.RegisterCourier;
using CourierService.Application.Commands.UpdateCourierStatus;
using CourierService.Application.Models;
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
        var command = new RegisterCourierCommand(new CreateCourierModel
        {
            FullName = "John Doe",
            Phone = "+1234567890",
            Email = "john@example.com",
            DocumentNumber = "12345"
        });

        // Act
        var result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterCourierCommandValidator_WithEmptyName_ShouldFail()
    {
        // Arrange
        var command = new RegisterCourierCommand(new CreateCourierModel
        {
            FullName = "",
            Phone = "+1234567890",
            Email = "john@example.com",
            DocumentNumber = "12345"
        });

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
        var command = new RegisterCourierCommand(new CreateCourierModel
        {
            FullName = new string('a', 201),
            Phone = "+1234567890",
            Email = "john@example.com",
            DocumentNumber = "12345"
        });

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
        var command = new RegisterCourierCommand(new CreateCourierModel
        {
            FullName = "John Doe",
            Phone = "+1234567890",
            Email = "invalid-email",
            DocumentNumber = "12345"
        });

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
        var command = new RegisterCourierCommand(new CreateCourierModel
        {
            FullName = "John Doe",
            Phone = "",
            Email = "john@example.com",
            DocumentNumber = "12345"
        });

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
            new UpdateCourierModel { Status = 1 }
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
            new UpdateCourierModel { Status = 1 }
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
            new UpdateCourierModel { Status = null }
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
            new UpdateCourierModel { Status = 999 } // Invalid status code
        );

        // Act
        var result = _updateValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Model.Status");
    }
}
