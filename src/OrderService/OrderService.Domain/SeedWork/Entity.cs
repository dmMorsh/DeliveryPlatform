namespace OrderService.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    public override bool Equals(object? obj)
        => obj is Entity other && Id == other.Id;

    public override int GetHashCode()
        => Id.GetHashCode();
}
