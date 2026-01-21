namespace ServiceName.Domain.SeedWork;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) {}
}