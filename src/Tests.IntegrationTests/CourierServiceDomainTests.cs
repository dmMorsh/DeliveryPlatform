using FluentAssertions;
using CourierService.Domain.Aggregates;

namespace Tests.IntegrationTests;

public class CourierServiceDomainTests
{
    [Fact]
    public void Register_WithValidData_ShouldCreateCourier()
    {
        // Arrange
        var fullName = "John Doe";
        var phone = "+1234567890";
        var email = "john@example.com";
        var docNumber = "DOC123";

        // Act
        var courier = Courier.Register(fullName, phone, email, docNumber);

        // Assert
        courier.Id.Should().NotBe(Guid.Empty);
        courier.FullName.Should().Be(fullName);
        courier.Phone.Should().Be(phone);
        courier.Email.Should().Be(email);
        courier.DocumentNumber.Should().Be(docNumber);
        courier.IsActive.Should().BeTrue();
        courier.Rating.Should().Be(5.0);
        courier.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ChangeStatus_WithValidStatus_ShouldUpdate()
    {
        // Arrange
        var courier = Courier.Register("John Doe", "+1234567890", "john@example.com", "DOC123");
        courier.ClearDomainEvents();

        // Act
        courier.ChangeStatus(CourierStatus.Online);

        // Assert
        courier.Status.Should().Be(CourierStatus.Online);
        courier.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var courier = Courier.Register("John Doe", "+1234567890", "john@example.com", "DOC123");
        courier.ClearDomainEvents();
        var currentStatus = courier.Status;

        // Act
        courier.ChangeStatus(currentStatus);

        // Assert
        courier.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var courier = Courier.Register("John Doe", "+1234567890", "john@example.com", "DOC123");

        // Act
        courier.Deactivate();

        // Assert
        courier.IsActive.Should().BeFalse();
    }
}
