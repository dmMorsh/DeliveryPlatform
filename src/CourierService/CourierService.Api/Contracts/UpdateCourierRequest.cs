namespace CourierService.Api.Contracts;

public record UpdateCourierRequest
{
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public bool? IsActive { get; set; }
}