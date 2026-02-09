namespace Tests.IntegrationTests;

public static class TestData
{
    public static Guid TestOrderId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid TestCourierId => Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static Guid TestCustomerId => Guid.Parse("00000000-0000-0000-0000-000000000003");

    public static string TestAddress => "123 Test Street, Test City";
    public static double TestLatitude => 40.7128;
    public static double TestLongitude => -74.0060;
}
