namespace DeliveryService.Application.Interfaces;

public interface ICourierDirectory
{
    Task<IReadOnlyList<CourierCandidate>> GetActiveCouriersAsync(CancellationToken ct = default);
}

public record CourierCandidate
{
    public Guid Id { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double Rating { get; init; }
    public DateTime? LastLocationUpdate { get; init; }
}
