using MediatR;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.ValueObjects;
using Shared.Utilities;

namespace CatalogService.Application.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<ProductView>>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateProductCommandHandler(IProductRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<ProductView>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var model = request.Model;

        if (string.IsNullOrWhiteSpace(model.Name))
            return ApiResponse<ProductView>.ErrorResponse("Product name is required");

        var money = new Money(model.PriceCents, model.Currency ?? "USD");
        var weight = new Weight(0); // Default weight
        var product = new Product(model.Name, model.Description ?? "", money, weight);

        await _repo.AddAsync(product, ct);

        var outboxMessages = product.DomainEvents
            .Select(de => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = product.Id,
                Type = de.GetType().Name,
                OccurredAt = DateTime.UtcNow
            })
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        product.ClearDomainEvents();

        var view = new ProductView(product.Id, product.Name, model.Description, product.PriceCents.AmountCents, product.PriceCents.Currency);
        return ApiResponse<ProductView>.SuccessResponse(view, "Product created successfully");
    }
}
