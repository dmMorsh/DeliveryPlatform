using FluentAssertions;
using LocationTrackingService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Tests.IntegrationTests;

/// <summary>
/// Tests for LocationService - the in-memory storage service for courier locations
/// </summary>
public class LocationServiceTests
{
    private readonly LocationService _locationService;
    private readonly Mock<ILogger<LocationService>> _mockLogger;
    private readonly Dictionary<RedisKey, RedisValue> _kv = new();
    private readonly Dictionary<RedisKey, List<RedisValue>> _lists = new();

    public LocationServiceTests()
    {
        var mux = BuildRedisMock();
        _mockLogger = new Mock<ILogger<LocationService>>();
        _locationService = new LocationService(_mockLogger.Object, mux);
    }

    [Fact]
    public async Task UpdateCourierLocationAsync_WithValidCoordinates_ShouldStoreLocation()
    {
        // Arrange
        var courierId = Guid.NewGuid(); // Use unique ID to avoid shared state issues
        var latitude = TestData.TestLatitude;
        var longitude = TestData.TestLongitude;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, timestamp);

        // Assert
        var location = await _locationService.GetCourierLocationAsync(courierId);
        location.Should().NotBeNull();
        location!.Latitude.Should().Be(latitude);
        location.Longitude.Should().Be(longitude);
    }

    [Fact]
    public async Task UpdateCourierLocationAsync_MultipleTimes_ShouldOverwritePreviousLocation()
    {
        // Arrange
        var courierId = Guid.NewGuid(); // Use unique ID to avoid shared state issues

        // Act - First update
        await _locationService.UpdateCourierLocationAsync(courierId, 40.0, -74.0, 10, DateTimeOffset.UtcNow);
        
        // Act - Second update with new coordinates
        await _locationService.UpdateCourierLocationAsync(courierId, 41.0, -75.0, 10, DateTimeOffset.UtcNow);

        // Assert - Should have latest location
        var location = await _locationService.GetCourierLocationAsync(courierId);
        location.Should().NotBeNull();
        location!.Latitude.Should().Be(41.0);
        location.Longitude.Should().Be(-75.0);
    }

    [Fact]
    public async Task UpdateCourierLocationAsync_MultipleCouriers_ShouldTrackIndependently()
    {
        // Arrange
        var courier1Id = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var courier2Id = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        await _locationService.UpdateCourierLocationAsync(courier1Id, 40.0, -74.0, 10, timestamp);
        await _locationService.UpdateCourierLocationAsync(courier2Id, 41.0, -75.0, 10, timestamp);

        // Assert
        var loc1 = await _locationService.GetCourierLocationAsync(courier1Id);
        var loc2 = await _locationService.GetCourierLocationAsync(courier2Id);

        loc1.Should().NotBeNull();
        loc2.Should().NotBeNull();
        loc1!.Latitude.Should().Be(40.0);
        loc1.Longitude.Should().Be(-74.0);
        loc2!.Latitude.Should().Be(41.0);
        loc2.Longitude.Should().Be(-75.0);
        loc1.Latitude.Should().NotBe(loc2.Latitude);
    }

    [Theory]
    [InlineData(40.7128, -74.0060)]      // New York
    [InlineData(51.5074, -0.1278)]       // London
    [InlineData(-33.8688, 151.2093)]     // Sydney
    public async Task UpdateCourierLocationAsync_WithVariousGlobalCoordinates_ShouldStoreCorrectly(double lat, double lon)
    {
        // Arrange
        var courierId = Guid.NewGuid(); // Use unique ID to avoid cross-test contamination

        // Act
        await _locationService.UpdateCourierLocationAsync(courierId, lat, lon, 10, DateTimeOffset.UtcNow);

        // Assert
        var location = await _locationService.GetCourierLocationAsync(courierId);
        location.Should().NotBeNull();
        location!.Latitude.Should().Be(lat);
        location.Longitude.Should().Be(lon);
    }

    [Fact]
    public async Task HighFrequencyUpdates_ShouldHandleMultipleSequentialUpdates()
    {
        // Arrange
        var courierId = Guid.NewGuid();

        // Act - Simulate 10 sequential location updates
        for (int i = 0; i < 10; i++)
        {
            var latitude = TestData.TestLatitude + (i * 0.001);
            var longitude = TestData.TestLongitude + (i * 0.001);
            await _locationService.UpdateCourierLocationAsync(courierId, latitude, longitude, 10, DateTimeOffset.UtcNow);
        }

        // Assert - Should have the final location (from iteration 9)
        var location = await _locationService.GetCourierLocationAsync(courierId);
        location.Should().NotBeNull();
        var expectedLat = TestData.TestLatitude + (9 * 0.001);
        var expectedLon = TestData.TestLongitude + (9 * 0.001);
        location!.Latitude.Should().BeApproximately(expectedLat, 0.0001);
        location.Longitude.Should().BeApproximately(expectedLon, 0.0001);
    }

    private IConnectionMultiplexer BuildRedisMock()
    {
        var db = new Mock<IDatabase>();
        var sub = new Mock<ISubscriber>();
        var mux = new Mock<IConnectionMultiplexer>();

        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((key, value, _, __, ___) =>
            {
                _kv[key] = value;
                return Task.FromResult(true);
            });

        db.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((key, value, _, __, ___, ____) =>
            {
                _kv[key] = value;
                return Task.FromResult(true);
            });

        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, _) =>
            {
                return Task.FromResult(_kv.TryGetValue(key, out var value) ? value : RedisValue.Null);
            });

        db.Setup(d => d.ListLeftPushAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, RedisValue, When, CommandFlags>((key, value, _, __) =>
            {
                if (!_lists.TryGetValue(key, out var list))
                {
                    list = new List<RedisValue>();
                    _lists[key] = list;
                }

                list.Insert(0, value);
                return Task.FromResult((long)list.Count);
            });

        db.Setup(d => d.ListRangeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CommandFlags>()))
            .Returns<RedisKey, long, long, CommandFlags>((key, start, stop, _) =>
            {
                if (!_lists.TryGetValue(key, out var list))
                    return Task.FromResult(Array.Empty<RedisValue>());

                var count = list.Count;
                var s = (int)Math.Max(0, start);
                var e = (int)Math.Min(stop, count - 1);
                if (count == 0 || s > e)
                    return Task.FromResult(Array.Empty<RedisValue>());

                return Task.FromResult(list.Skip(s).Take(e - s + 1).ToArray());
            });

        db.Setup(d => d.ListTrimAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        db.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));

        sub.Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(0L));

        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(db.Object);
        mux.Setup(m => m.GetSubscriber(It.IsAny<object?>())).Returns(sub.Object);

        return mux.Object;
    }
}
