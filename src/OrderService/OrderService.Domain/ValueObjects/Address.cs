namespace OrderService.Domain.ValueObjects;

public class Address : ValueObject
{
    private Address() { }
    public string Street { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public Address(string street, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty", nameof(street));

        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Street = street;
        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Latitude;
        yield return Longitude;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Address other) return false;
        return Street == other.Street && Latitude == other.Latitude && Longitude == other.Longitude;
    }

    public override int GetHashCode() => HashCode.Combine(Street, Latitude, Longitude);
}
