namespace Shared.Services;

/// <summary>
/// gRPC клиент для LocationTrackingService
/// </summary>
public interface ILocationTrackingClient
{
    Task<bool> UpdateCourierLocationAsync(Guid courierId, double latitude, double longitude, int accuracy = 0);
    Task<(double Latitude, double Longitude, bool IsOnline)> GetCourierLocationAsync(Guid courierId);
}
