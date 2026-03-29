namespace Shared.Services;

/// <summary>
/// gRPC client for LocationTrackingService
/// </summary>
public interface ILocationTrackingClient
{
    Task<bool> UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy = 0);
    Task<(double Latitude, double Longitude, bool IsOnline)> GetCourierLocationAsync(Guid courierId);
    Task<IReadOnlyList<(double Latitude, double Longitude, long TimestampMs, int Accuracy)>> GetCourierLocationHistoryAsync(
        Guid courierId,
        long fromTimestampMs = 0,
        int limit = 100);
}
