using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.Models;

public class CreateOrderModel
{
    [Required(ErrorMessage = "ClientId is required")]
    public Guid ClientId { get; set; }

    [Required(ErrorMessage = "FromAddress is required")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "FromAddress must be between 3 and 500 characters")]
    public string FromAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "ToAddress is required")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "ToAddress must be between 3 and 500 characters")]
    public string ToAddress { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "FromLatitude must be between -90 and 90")]
    public double FromLatitude { get; set; }

    [Range(-180, 180, ErrorMessage = "FromLongitude must be between -180 and 180")]
    public double FromLongitude { get; set; }

    [Range(-90, 90, ErrorMessage = "ToLatitude must be between -90 and 90")]
    public double ToLatitude { get; set; }

    [Range(-180, 180, ErrorMessage = "ToLongitude must be between -180 and 180")]
    public double ToLongitude { get; set; }

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "WeightGrams must be greater than 0")]
    public int WeightGrams { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "CostCents must be greater than 0")]
    public long CostCents { get; set; }

    [StringLength(500, ErrorMessage = "CourierNote must not exceed 500 characters")]
    public string? CourierNote { get; set; }
}