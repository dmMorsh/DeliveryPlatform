using CatalogService.Domain.SeedWork;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Events;

public class ProductPriceChanged(Guid id, Money newPrice) : DomainEvent;