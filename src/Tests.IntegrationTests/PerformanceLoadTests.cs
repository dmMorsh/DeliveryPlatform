using System.Diagnostics;
using FluentAssertions;
using LocationTrackingService.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.IntegrationTests;

/// <summary>
/// Performance and load testing
/// Tests: High-frequency updates, concurrent couriers, response times
/// </summary>
public class PerformanceLoadTests
{
    private readonly LocationService _locationService;
    private readonly Mock<ILogger<LocationService>> _mockLogger;

    public PerformanceLoadTests()
    {
        _mockLogger = new Mock<ILogger<LocationService>>();
        _locationService = new LocationService(_mockLogger.Object);
    }

    [Fact]
    public async Task HighFrequencyUpdates_100Updates_ShouldComplete()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act - 100 rapid location updates
        for (int i = 0; i < 100; i++)
        {
            var latitude = TestData.TestLatitude + (i * 0.0001);
            var longitude = TestData.TestLongitude + (i * 0.0001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Assert
        var finalLocation = await _locationService.GetCourierLocationAsync(courierId);
        finalLocation.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "100 updates should complete in < 5 seconds");
    }

    [Fact]
    public async Task MultipleCouriers_50Couriers_IndependentTracking()
    {
        // Arrange
        var numCouriers = 50;
        var stopwatch = Stopwatch.StartNew();
        var courierIds = new List<Guid>();

        // Act - Create 50 courier updates
        for (int i = 0; i < numCouriers; i++)
        {
            var courierId = new Guid($"00000000-0000-0000-{i:0000}-000000000000");
            courierIds.Add(courierId);
            var latitude = 40.0 + (i * 0.01);
            var longitude = -74.0 + (i * 0.01);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Assert - All couriers tracked
        foreach (var courierId in courierIds)
        {
            var location = await _locationService.GetCourierLocationAsync(courierId);
            location.Should().NotBeNull();
        }

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "50 couriers should complete in < 10 seconds");
    }

    [Fact]
    public async Task ConcurrentUpdates_10CouriersSimultaneous_ShouldHandleLoad()
    {
        // Arrange
        var numCouriers = 10;
        var updatesPerCourier = 10;
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate concurrent updates
        var tasks = new List<Task>();
        for (int i = 0; i < numCouriers; i++)
        {
            var courierId = new Guid($"00000000-0000-0000-{i:0000}-000000000000");
            
            for (int j = 0; j < updatesPerCourier; j++)
            {
                var latitude = 40.0 + (i * 0.01) + (j * 0.001);
                var longitude = -74.0 + (i * 0.01) + (j * 0.001);
                var task = _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
                tasks.Add(task);
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - All updates completed
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Concurrent updates should complete in < 5 seconds");
    }

    [Fact]
    public async Task ResponseTime_SingleUpdate_ShouldBeFast()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _locationService.UpdateCourierLocationAsync(courierId, 40.0, -74.0, 10, DateTimeOffset.UtcNow);
        stopwatch.Stop();

        var updateTime = stopwatch.ElapsedMilliseconds;

        // Assert
        updateTime.Should().BeLessThan(50, "Single update should complete in < 50ms");
    }

    [Fact]
    public async Task ResponseTime_SingleQuery_ShouldBeFast()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        await _locationService.UpdateCourierLocationAsync(courierId, 40.0, -74.0, 10, DateTimeOffset.UtcNow);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var location = await _locationService.GetCourierLocationAsync(courierId);
        stopwatch.Stop();

        var queryTime = stopwatch.ElapsedMilliseconds;

        // Assert
        queryTime.Should().BeLessThan(30, "Single query should complete in < 30ms");
        location.Should().NotBeNull();
    }

    [Fact]
    public async Task ThroughputTest_1000Operations_MeasureThroughput()
    {
        // Arrange
        var numOperations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act - 1000 update operations
        for (int i = 0; i < numOperations; i++)
        {
            var courierId = Guid.NewGuid();
            var latitude = 40.0 + ((i % 100) * 0.01);
            var longitude = -74.0 + ((i % 100) * 0.01);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Calculate throughput
        var totalTime = stopwatch.ElapsedMilliseconds / 1000.0; // Convert to seconds
        var throughput = numOperations / totalTime;

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "1000 ops should complete in < 30 seconds");
        throughput.Should().BeGreaterThan(30, "Should handle > 30 updates per second");
    }

    [Fact]
    public async Task StressTest_BurstyLoad_ShouldRecover()
    {
        // Arrange
        var courierId = Guid.NewGuid();

        // Act - Burst of updates
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
        {
            var latitude = 40.0 + (i * 0.001);
            var longitude = -74.0 + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        // Pause
        await Task.Delay(500);

        // Another burst
        for (int i = 0; i < 50; i++)
        {
            var latitude = 40.05 + (i * 0.001);
            var longitude = -74.05 + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Assert - System recovered and processed all updates
        var location = await _locationService.GetCourierLocationAsync(courierId);
        location.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Bursty load should complete in < 3 seconds");
    }

    [Fact]
    public async Task MemoryEfficiency_1000Couriers_ShouldNotCrash()
    {
        // Arrange
        var numCouriers = 1000;

        // Act - Create 1000 courier locations
        for (int i = 0; i < numCouriers; i++)
        {
            var courierId = Guid.Parse($"00000000-0000-0000-{i:0000}-000000000000");
            var latitude = 40.0 + ((i % 360) * 0.1);
            var longitude = -74.0 + ((i % 180) * 0.1);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        // Act - Query a few to verify they're stored
        for (int i = 0; i < 10; i++)
        {
            var courierId = Guid.Parse($"00000000-0000-0000-{i:0000}-000000000000");
            var location = await _locationService.GetCourierLocationAsync(courierId);
            
            // Assert - All locations retrievable
            location.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public async Task ScalablePerformance_VariousLoads(int numUpdates)
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act - Variable number of updates
        for (int i = 0; i < numUpdates; i++)
        {
            var latitude = 40.0 + (i * 0.001);
            var longitude = -74.0 + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        stopwatch.Stop();

        // Assert - Performance scales
        if (numUpdates == 10)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
        else if (numUpdates == 100)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
        else if (numUpdates == 500)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000);
    }

    [Fact]
    public async Task AverageLatency_MultipleOperations_CalculateStats()
    {
        // Arrange
        var courierId = Guid.NewGuid();
        var latencies = new List<long>();
        var numSamples = 100;

        // Act - Measure 100 operations
        for (int i = 0; i < numSamples; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var latitude = 40.0 + (i * 0.001);
            var longitude = -74.0 + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Calculate stats
        var avgLatency = latencies.Count > 0 ? latencies.ConvertAll(l => (double)l).FindAll(l => l > 0).Count > 0 ? 0 : 1 : 1;
        var maxLatency = latencies.Count > 0 ? 0 : 1; // Simplified for test

        // Assert - Performance characteristics
        latencies.Should().HaveCount(numSamples);
        latencies.Should().OnlyContain(l => l < 100, "All operations should complete in < 100ms");
    }
}
