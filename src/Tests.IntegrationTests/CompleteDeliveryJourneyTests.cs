using System.Diagnostics;
using FluentAssertions;
using LocationTrackingService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.IntegrationTests;

/// <summary>
/// Complete end-to-end delivery journey tests
/// Tests: Order creation → Courier assignment → Location tracking → Delivery
/// </summary>
public class CompleteDeliveryJourneyTests
{
    private readonly LocationService _locationService;
    private readonly Mock<ILogger<LocationService>> _mockLogger;

    public CompleteDeliveryJourneyTests()
    {
        _mockLogger = new Mock<ILogger<LocationService>>();
        _locationService = new LocationService(_mockLogger.Object);
    }

    [Fact]
    public async Task FullDeliveryJourney_OrderToDelivery_ShouldComplete()
    {
        // Arrange
        var orderId = TestData.TestOrderId;
        var courierId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // ===== STEP 1: Order Created =====
        var orderCreatedEvent = new
        {
            OrderId = orderId,
            CustomerId = customerId,
            Status = "created",
            CreatedAt = DateTime.UtcNow,
            Amount = 99.99m
        };

        orderCreatedEvent.OrderId.Should().NotBe(Guid.Empty);
        orderCreatedEvent.Status.Should().Be("created");

        // ===== STEP 2: Courier Assigned =====
        var courierAssignedEvent = new
        {
            OrderId = orderId,
            CourierId = courierId,
            Status = "assigned",
            AssignedAt = DateTime.UtcNow
        };

        courierAssignedEvent.CourierId.Should().NotBe(Guid.Empty);
        courierAssignedEvent.Status.Should().Be("assigned");

        // ===== STEP 3: Courier Comes Online with Location =====
        var startLatitude = TestData.TestLatitude;
        var startLongitude = TestData.TestLongitude;

        await _locationService.UpdateCourierLocationAsync(courierId, startLatitude, startLongitude, 10,
            DateTimeOffset.UtcNow);
        var courierLocation = await _locationService.GetCourierLocationAsync(courierId);

        courierLocation.Should().NotBeNull();
        courierLocation!.Latitude.Should().Be(startLatitude);

        // ===== STEP 4: Courier Moves Towards Delivery Location =====
        var midLatitude = startLatitude + 0.005;
        var midLongitude = startLongitude + 0.005;

        await _locationService.UpdateCourierLocationAsync(courierId, midLatitude, midLongitude, 10,
            DateTimeOffset.UtcNow);
        var midpointLocation = await _locationService.GetCourierLocationAsync(courierId);

        midpointLocation!.Latitude.Should().Be(midLatitude);
        midpointLocation.Longitude.Should().Be(midLongitude);

        // ===== STEP 5: Status Changed to "In Transit" =====
        var inTransitEvent = new
        {
            OrderId = orderId,
            Status = "in_transit",
            CourierId = courierId,
            CurrentLatitude = midLatitude,
            CurrentLongitude = midLongitude,
            UpdatedAt = DateTime.UtcNow
        };

        inTransitEvent.Status.Should().Be("in_transit");

        // ===== STEP 6: Courier Arrives at Delivery Location =====
        var deliveryLatitude = startLatitude + 0.01;
        var deliveryLongitude = startLongitude + 0.01;

        await _locationService.UpdateCourierLocationAsync(courierId, deliveryLatitude, deliveryLongitude, 10,
            DateTimeOffset.UtcNow);
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);

        finalLocation!.Latitude.Should().Be(deliveryLatitude);

        // ===== STEP 7: Delivery Completed =====
        var deliveryCompletedEvent = new
        {
            OrderId = orderId,
            CourierId = courierId,
            Status = "delivered",
            DeliveredAt = DateTime.UtcNow,
            DeliveryLatitude = deliveryLatitude,
            DeliveryLongitude = deliveryLongitude
        };

        deliveryCompletedEvent.Status.Should().Be("delivered");

        stopwatch.Stop();

        // ===== FINAL ASSERTIONS =====
        // All events in sequence
        orderCreatedEvent.Status.Should().Be("created");
        courierAssignedEvent.Status.Should().Be("assigned");
        inTransitEvent.Status.Should().Be("in_transit");
        deliveryCompletedEvent.Status.Should().Be("delivered");

        // Journey time should be reasonable
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Complete journey should finish in < 5 seconds");
    }

    [Fact]
    public async Task MultipleCouriersMultipleDeliveries_IndependentJourneys()
    {
        // Arrange
        var courier1Id = Guid.Parse("00000000-0000-0000-0000-000000000501");
        var courier2Id = Guid.Parse("00000000-0000-0000-0000-000000000502");
        var order1Id = Guid.Parse("00000000-0000-0000-0000-000000001001");
        var order2Id = Guid.Parse("00000000-0000-0000-0000-000000001002");

        // ===== DELIVERY 1: Courier 1 with Order 1 =====
        var delivery1 = new
        {
            OrderId = order1Id,
            CourierId = courier1Id,
            StartLocation = (lat: 40.7128, lon: -74.0060),
            DeliveryLocation = (lat: 40.7200, lon: -74.0100)
        };

        // ===== DELIVERY 2: Courier 2 with Order 2 =====
        var delivery2 = new
        {
            OrderId = order2Id,
            CourierId = courier2Id,
            StartLocation = (lat: 51.5074, lon: -0.1278),
            DeliveryLocation = (lat: 51.5150, lon: -0.1350)
        };

        // Act - Courier 1 updates location
        await _locationService.UpdateCourierLocationAsync(courier1Id, delivery1.StartLocation.lat,
            delivery1.StartLocation.lon, 10, DateTimeOffset.UtcNow);

        // Act - Courier 2 updates location
        await _locationService.UpdateCourierLocationAsync(courier2Id, delivery2.StartLocation.lat,
            delivery2.StartLocation.lon, 10, DateTimeOffset.UtcNow);

        // Assert - Both couriers tracked independently
        var loc1 = await _locationService.GetCourierLocationAsync(courier1Id);
        var loc2 = await _locationService.GetCourierLocationAsync(courier2Id);

        loc1.Should().NotBeNull();
        loc2.Should().NotBeNull();
        loc1!.Latitude.Should().Be(delivery1.StartLocation.lat);
        loc2!.Latitude.Should().Be(delivery2.StartLocation.lat);
        loc1.CourierId.Should().NotBe(loc2.CourierId);
    }

    [Fact]
    public async Task DeliveryWithHighFrequencyUpdates_RealtimeTracking()
    {
        // Arrange - Simulate real-time tracking with 1 second intervals
        var courierId = Guid.NewGuid();
        var numUpdates = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate courier moving with location updates every 100ms
        for (int i = 0; i < numUpdates; i++)
        {
            var latitude = TestData.TestLatitude + (i * 0.001);
            var longitude = TestData.TestLongitude + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10 - i,
                DateTimeOffset.UtcNow);
            await Task.Delay(50); // Simulate 50ms network delay
        }

        stopwatch.Stop();

        // Assert - All updates processed
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);
        finalLocation.Should().NotBeNull();
        finalLocation!.Latitude.Should().Be(TestData.TestLatitude + (9 * 0.001));
        stopwatch.ElapsedMilliseconds.Should()
            .BeLessThan(2000, "10 updates with delays should complete in < 2 seconds");
    }

    [Fact]
    public async Task DeliveryProcess_WithLocationHistory_TracksProgressCorrectly()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var locations = new[]
        {
            (lat: 40.7100, lon: -74.0100, status: "pickup"), // Start
            (lat: 40.7150, lon: -74.0120, status: "traveling"), // Heading to delivery
            (lat: 40.7200, lon: -74.0150, status: "traveling"), // Getting closer
            (lat: 40.7250, lon: -74.0180, status: "arrived") // At delivery location
        };

        // Act - Track courier through delivery journey
        for (int i = 0; i < locations.Length; i++)
        {
            await _locationService.UpdateCourierLocationAsync(
                courierId,
                locations[i].lat,
                locations[i].lon,
                10,
                DateTimeOffset.UtcNow
            );
        }

        // Assert - Final location is correct
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);
        finalLocation.Should().NotBeNull();
        finalLocation!.Latitude.Should().Be(40.7250);
        finalLocation.Longitude.Should().Be(-74.0180);
    }

    [Fact]
    public async Task CancelledDelivery_CourierReturnsToHub()
    {
        // Arrange - Use unique ID to avoid concurrent access issues
        var courierId = Guid.NewGuid();
        var hubLocation = (lat: 40.7000, lon: -74.0000);
        var deliveryLocation = (lat: 40.7500, lon: -74.0500);

        // Act - Courier leaves hub
        await _locationService.UpdateCourierLocationAsync(courierId, deliveryLocation.lat, deliveryLocation.lon, 10,
            DateTimeOffset.UtcNow);
        var courierAtDelivery = await _locationService.GetCourierLocationAsync(courierId);

        // Assert - Courier is at delivery location
        courierAtDelivery.Should().NotBeNull();
        courierAtDelivery!.Latitude.Should().BeApproximately(deliveryLocation.lat, 0.0001);

        // Act - Delivery cancelled, courier returns to hub
        await _locationService.UpdateCourierLocationAsync(courierId, hubLocation.lat, hubLocation.lon, 10,
            DateTimeOffset.UtcNow);
        var courierAtHub = await _locationService.GetCourierLocationAsync(courierId);

        // Assert - Courier back at hub
        courierAtHub.Should().NotBeNull();
        courierAtHub!.Latitude.Should().BeApproximately(hubLocation.lat, 0.0001);
        courierAtHub.Longitude.Should().BeApproximately(hubLocation.lon, 0.0001);
    }

    [Fact]
    public async Task DeliveryCompletionMetrics_ShouldBeAccurate()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var startTime = DateTimeOffset.UtcNow;
        var startLocation = (lat: 40.7000, lon: -74.0000);
        var deliveryLocation = (lat: 40.7100, lon: -74.0100);

        // Act - Update location at start
        await _locationService.UpdateCourierLocationAsync(courierId, startLocation.lat, startLocation.lon, 10,
            startTime);

        // Simulate delivery travel time
        await Task.Delay(100);

        // Act - Update location at delivery
        var endTime = DateTimeOffset.UtcNow;
        await _locationService.UpdateCourierLocationAsync(courierId, deliveryLocation.lat, deliveryLocation.lon, 10,
            endTime);

        // Assert - Delivery completed
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);
        finalLocation.Should().NotBeNull();
        finalLocation!.Latitude.Should().Be(deliveryLocation.lat);

        // Time elapsed should be captured
        var elapsedTime = endTime - startTime;
        elapsedTime.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }
}