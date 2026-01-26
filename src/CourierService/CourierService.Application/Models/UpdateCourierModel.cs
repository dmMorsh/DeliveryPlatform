namespace CourierService.Application.Models;

public class UpdateCourierModel
{
    public int? Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public bool? IsActive { get; set; }
}