using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Models;
using CatalogService.Application.Services;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.ValueObjects;
using MediatR;
using Shared.Contracts;

namespace CatalogService.Application.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<ProductView>>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IProductIntegrationEventMapper _eventMapper;

    public CreateProductCommandHandler(IProductRepository repo, IUnitOfWork uow, IProductIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<ProductView>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ApiResponse<ProductView>.ErrorResponse("Product name is required");

        var money = new Money(request.PriceCents, request.Currency ?? "USD");
        var weight = new Weight(request.WeightGrams);
        var product = new Product(request.Name, request.Description ?? "", money, weight);

        await _repo.AddAsync(product, ct);

        var outboxMessages = product.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        product.ClearDomainEvents();
        ProductReadCache.Invalidate(product.Id);

        var view = ProductViewFactory.FromProduct(product);
        return ApiResponse<ProductView>.SuccessResponse(view, "Product created successfully");
    }
}
