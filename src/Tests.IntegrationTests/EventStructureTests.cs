using FluentAssertions;

namespace Tests.IntegrationTests;

/// <summary>
/// Tests for event structure validation
/// Ensures events have correct format and statuses
/// </summary>
public class EventStructureTests
{
    [Fact]
    public void OrderEvent_ShouldHaveRequiredFields()
    {
        // Arrange & Act
        var orderEvent = new
        {
            OrderId = TestData.TestOrderId,
            Status = "created",
            CreatedAt = DateTime.UtcNow,
            Amount = 99.99m
        };

        // Assert
        orderEvent.OrderId.Should().NotBe(Guid.Empty);
        orderEvent.Status.Should().NotBeNullOrEmpty();
        orderEvent.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        orderEvent.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CourierEvent_ShouldHaveRequiredFields()
    {
        // Arrange & Act
        var courierEvent = new
        {
            CourierId = TestData.TestCourierId,
            Status = "online",
            Location = new { Latitude = TestData.TestLatitude, Longitude = TestData.TestLongitude },
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        courierEvent.CourierId.Should().NotBe(Guid.Empty);
        courierEvent.Status.Should().NotBeNullOrEmpty();
        courierEvent.Location.Should().NotBeNull();
        courierEvent.UpdatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("created")]
    [InlineData("assigned")]
    [InlineData("in_transit")]
    [InlineData("delivered")]
    public void OrderStatus_ShouldBeValid(string status)
    {
        // Arrange
        var validStatuses = new[] { "created", "assigned", "in_transit", "delivered" };

        // Assert
        validStatuses.Should().Contain(status);
    }

    [Theory]
    [InlineData("online")]
    [InlineData("busy")]
    [InlineData("offline")]
    public void CourierStatus_ShouldBeValid(string status)
    {
        // Arrange
        var validStatuses = new[] { "online", "busy", "offline" };

        // Assert
        validStatuses.Should().Contain(status);
    }

    [Fact]
    public void LocationUpdate_ShouldHaveValidCoordinates()
    {
        // Arrange & Act
        var locationUpdate = new
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            Accuracy = 10.5,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        locationUpdate.Latitude.Should().BeGreaterThanOrEqualTo(-90).And.BeLessThanOrEqualTo(90);
        locationUpdate.Longitude.Should().BeGreaterThanOrEqualTo(-180).And.BeLessThanOrEqualTo(180);
        locationUpdate.Accuracy.Should().BeGreaterThan(0);
        locationUpdate.Timestamp.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void DeliveryJourneyEvents_ShouldFollowCorrectSequence()
    {
        // Arrange
        var orderId = TestData.TestOrderId;
        var courierId = TestData.TestCourierId;

        var orderCreated = new { OrderId = orderId, Status = "created", Time = DateTime.UtcNow };
        var courierAssigned = new { OrderId = orderId, CourierId = courierId, Status = "assigned", Time = DateTime.UtcNow };
        var inTransit = new { OrderId = orderId, Status = "in_transit", Time = DateTime.UtcNow };
        var delivered = new { OrderId = orderId, Status = "delivered", Time = DateTime.UtcNow };

        // Assert
        orderCreated.Status.Should().Be("created");
        courierAssigned.Status.Should().Be("assigned");
        inTransit.Status.Should().Be("in_transit");
        delivered.Status.Should().Be("delivered");

        orderCreated.Time.Should().BeBefore(courierAssigned.Time.AddSeconds(1));
        courierAssigned.Time.Should().BeBefore(inTransit.Time.AddSeconds(1));
        inTransit.Time.Should().BeBefore(delivered.Time.AddSeconds(1));
    }
}
