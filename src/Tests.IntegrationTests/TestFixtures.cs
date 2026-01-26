using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LocationTrackingService.Services;

namespace Tests.IntegrationTests;

/// <summary>
/// Shared fixture for clearing LocationService state between tests
/// </summary>
public class LocationServiceFixture : IDisposable
{
    private readonly LocationService _locationService;

    public LocationServiceFixture()
    {
        var logger = new Mock<ILogger<LocationService>>().Object;
        _locationService = new LocationService(logger);
        ClearState();
    }

    public void ClearState()
    {
        _locationService.ClearLocationStore();
    }

    public void Dispose()
    {
        ClearState();
    }
}

/// <summary>
/// Collection definition for location service tests to share cleanup
/// </summary>
[CollectionDefinition("LocationService Collection")]
public class LocationServiceCollection : ICollectionFixture<LocationServiceFixture>
{
    // This has no code, just defines the collection
}

/// <summary>
/// Базовые fixtures для интеграционных тестов
/// </summary>
public class TestFixtures
{
    /// <summary>
    /// Mock конфигурация для тестов
    /// </summary>
    public static IConfiguration CreateMockConfiguration()
    {
        var configDictionary = new Dictionary<string, string>
        {
            { "gRPC:LocationTrackingService:Url", "https://localhost:7070" },
            { "Kafka:Bootstrap", "localhost:9092" },
            { "Kafka:GroupId", "test-group" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDictionary)
            .Build();

        return config;
    }

    /// <summary>
    /// Mock logger для тестов
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>() where T : class
    {
        return new Mock<ILogger<T>>().Object;
    }

    /// <summary>
    /// Mock logger factory
    /// </summary>
    public static ILoggerFactory CreateMockLoggerFactory()
    {
        return new Mock<ILoggerFactory>().Object;
    }

    /// <summary>
    /// Cleanup method to clear shared state between tests
    /// </summary>
    public static void CleanupLocationStore()
    {
        // Try to get and clear the LocationService static store
        try
        {
            var locationType = Type.GetType("LocationTrackingService.Services.LocationService, LocationTrackingService");
            if (locationType != null)
            {
                var method = locationType.GetMethod("ClearLocationStore", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method == null)
                {
                    // If static method doesn't exist, the instance method will be called via reflection
                    var logger = CreateMockLogger<LocationService>();
                    var service = (LocationService)Activator.CreateInstance(locationType, logger)!;
                    service.ClearLocationStore();
                }
            }
        }
        catch { /* Ignore if cleanup fails */ }
    }
}

/// <summary>
/// Тестовые данные
/// </summary>
public static class TestData
{
    public static Guid TestOrderId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid TestCourierId => Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static Guid TestCustomerId => Guid.Parse("00000000-0000-0000-0000-000000000003");
    
    public static string TestAddress => "123 Test Street, Test City";
    public static double TestLatitude => 40.7128;
    public static double TestLongitude => -74.0060;
    
    public static class OrderTests
    {
        public static (Guid, string) GetTestOrderData()
        {
            return (TestOrderId, TestAddress);
        }
    }

    public static class CourierTests
    {
        public static (Guid, double, double) GetTestLocationData()
        {
            return (TestCourierId, TestLatitude, TestLongitude);
        }
    }
}
