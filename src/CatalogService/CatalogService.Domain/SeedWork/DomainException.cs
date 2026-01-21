namespace CatalogService.Domain.SeedWork;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) {}
}