namespace CourierService.Application.Models;

public record CourierView
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public int Status { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public double Rating { get; set; }
    public int CompletedDeliveries { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}