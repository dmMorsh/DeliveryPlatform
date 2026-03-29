namespace GatewayApi.Contracts.Couriers;

/// <summary>
/// DTO for proxying courier update requests
/// </summary>
public class UpdateCourierRequest
{
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public double? Rating { get; set; }
}
