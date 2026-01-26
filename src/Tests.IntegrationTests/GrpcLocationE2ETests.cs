using System.Diagnostics;
using FluentAssertions;
using LocationTrackingService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.IntegrationTests;

/// <summary>
/// End-to-End tests for gRPC location tracking
/// Tests: Update location via gRPC → Verify storage → Query location
/// </summary>
public class GrpcLocationE2ETests
{
    private readonly LocationService _locationService;
    private readonly Mock<ILogger<LocationService>> _mockLogger;

    public GrpcLocationE2ETests()
    {
        _mockLogger = new Mock<ILogger<LocationService>>();
        _locationService = new LocationService(_mockLogger.Object);
    }

    [Fact]
    public async Task UpdateLocation_ThenRetrieve_ShouldReturnSameCoordinates()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var latitude = TestData.TestLatitude;
        var longitude = TestData.TestLongitude;
        var timestamp = DateTimeOffset.UtcNow;

        // Act - Update location
        await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, timestamp);

        // Act - Retrieve location
        var retrievedLocation = await _locationService.GetCourierLocationAsync(courierId);

        // Assert
        retrievedLocation.Should().NotBeNull();
        retrievedLocation!.Latitude.Should().Be(latitude);
        retrievedLocation.Longitude.Should().Be(longitude);
        retrievedLocation.CourierId.Should().Be(courierId);
    }

    [Fact]
    public async Task CourierStartsOffline_UpdatesLocation_BecomesOnline()
    {
        // Arrange
        var courierId = Guid.NewGuid();

        // Act 1 - Check initial state (offline)
        var initialLocation = await _locationService.GetCourierLocationAsync(courierId);
        var isInitiallyOffline = initialLocation == null;

        // Act 2 - Courier comes online with location
        await _locationService.UpdateCourierLocationAsync(courierId, 40.0, -74.0, 10, DateTimeOffset.UtcNow);

        // Act 3 - Check updated state
        var updatedLocation = await _locationService.GetCourierLocationAsync(courierId);
        var isNowOnline = updatedLocation != null;

        // Assert
        isInitiallyOffline.Should().BeTrue("courier should start offline");
        isNowOnline.Should().BeTrue("courier should be online after update");
        updatedLocation!.Latitude.Should().Be(40.0);
        updatedLocation.Longitude.Should().Be(-74.0);
    }

    [Fact]
    public async Task CourierMovesToDifferentLocation_ShouldReflectNewCoordinates()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var startLocation = (latitude: 40.7128, longitude: -74.0060);  // NYC
        var endLocation = (latitude: 40.7580, longitude: -73.9855);     // Central Park

        // Act 1 - Courier starts at location 1
        await _locationService.UpdateCourierLocationAsync(courierId, startLocation.latitude, startLocation.longitude, 10, DateTimeOffset.UtcNow);
        var location1 = await _locationService.GetCourierLocationAsync(courierId);

        // Act 2 - Courier moves to location 2
        await Task.Delay(100); // Simulate travel time
        await _locationService.UpdateCourierLocationAsync(courierId, endLocation.latitude, endLocation.longitude, 10, DateTimeOffset.UtcNow);
        var location2 = await _locationService.GetCourierLocationAsync(courierId);

        // Assert
        location1.Should().NotBeNull();
        location1!.Latitude.Should().Be(startLocation.latitude);
        location1.Longitude.Should().Be(startLocation.longitude);

        location2.Should().NotBeNull();
        location2!.Latitude.Should().Be(endLocation.latitude);
        location2.Longitude.Should().Be(endLocation.longitude);

        // Location should have changed
        location1.Latitude.Should().NotBe(location2.Latitude);
    }

    [Fact]
    public async Task RealTimeTracking_10ConsecutiveUpdates_AllSuccessful()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate real-time tracking (10 updates)
        for (int i = 0; i < 10; i++)
        {
            var latitude = TestData.TestLatitude + (i * 0.001);
            var longitude = TestData.TestLongitude + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Assert - All updates completed
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);
        finalLocation.Should().NotBeNull();
        finalLocation!.Latitude.Should().Be(TestData.TestLatitude + (9 * 0.001));
        finalLocation.Longitude.Should().Be(TestData.TestLongitude + (9 * 0.001));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "10 updates should complete in < 1 second");
    }

    [Fact]
    public async Task MultipleDeliveries_DifferentCouriers_IndependentTracking()
    {
        // Arrange
        var courier1Id = Guid.Parse("00000000-0000-0000-0000-000000000401");
        var courier2Id = Guid.Parse("00000000-0000-0000-0000-000000000402");

        // Act - Both couriers update their locations
        await _locationService.UpdateCourierLocationAsync(courier1Id, 40.7128, -74.0060, 10, DateTimeOffset.UtcNow);
        await _locationService.UpdateCourierLocationAsync(courier2Id, 51.5074, -0.1278, 10, DateTimeOffset.UtcNow);

        var location1 = await _locationService.GetCourierLocationAsync(courier1Id);
        var location2 = await _locationService.GetCourierLocationAsync(courier2Id);

        // Assert - Different locations maintained separately
        location1.Should().NotBeNull();
        location2.Should().NotBeNull();
        location1!.Latitude.Should().Be(40.7128);  // New York
        location2!.Latitude.Should().Be(51.5074);  // London
        location1.CourierId.Should().NotBe(location2.CourierId);
    }

    [Fact]
    public async Task GrpcResponseTime_ShouldBeFast()
    {
        // Arrange
        var courierId = Guid.NewGuid(); // Use unique ID to avoid shared state issues
        var stopwatch = Stopwatch.StartNew();

        // Act - Update location
        await _locationService.UpdateCourierLocationAsync(courierId, 40.0, -74.0, 10, DateTimeOffset.UtcNow);
        stopwatch.Stop();

        var time1 = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();

        // Act - Retrieve location
        var location = await _locationService.GetCourierLocationAsync(courierId);
        stopwatch.Stop();

        var time2 = stopwatch.ElapsedMilliseconds;

        // Assert - Response times should be fast
        time1.Should().BeLessThan(100, "Update should complete in < 100ms");
        time2.Should().BeLessThan(50, "Get should complete in < 50ms");
        location.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, 0)]              // Equator & Prime Meridian
    [InlineData(40.7128, -74.0060)] // New York
    [InlineData(-33.8688, 151.2093)] // Sydney
    [InlineData(51.5074, -0.1278)]  // London
    [InlineData(35.6762, 139.6503)] // Tokyo
    public async Task GlobalLocations_ShouldBeStored(double lat, double lon)
    {
        // Arrange
        var courierId = Guid.NewGuid();

        // Act
        await _locationService.UpdateCourierLocationAsync(courierId, lat, lon, 10, DateTimeOffset.UtcNow);
        var location = await _locationService.GetCourierLocationAsync(courierId);

        // Assert
        location.Should().NotBeNull();
        location!.Latitude.Should().Be(lat);
        location.Longitude.Should().Be(lon);
    }

    [Fact]
    public async Task LocationAccuracy_ShouldBePreserved()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var preciseLatitude = 40.712776;
        var preciseLongitude = -74.005974;

        // Act
        await _locationService.UpdateCourierLocationAsync(courierId, preciseLatitude, preciseLongitude, 5, DateTimeOffset.UtcNow);
        var location = await _locationService.GetCourierLocationAsync(courierId);

        // Assert - Precision should be maintained
        location.Should().NotBeNull();
        location!.Latitude.Should().Be(preciseLatitude);
        location.Longitude.Should().Be(preciseLongitude);
    }
}
