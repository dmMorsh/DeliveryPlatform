using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Models;
using CatalogService.Application.Services;
using CatalogService.Domain.ValueObjects;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductView>>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IProductIntegrationEventMapper _eventMapper;

    public UpdateProductCommandHandler(IProductRepository repo, IUnitOfWork uow, IProductIntegrationEventMapper eventMapper)
    {
        _repo = repo;
        _uow = uow;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse<ProductView>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.ProductId, ct);

        if (product == null)
            return ApiResponse<ProductView>.ErrorResponse("Product not found");
        
        if (request.PriceCents.HasValue && product.PriceCents.AmountCents != request.PriceCents)
        {
            var newPrice = new Money(request.PriceCents.Value, request.Currency ?? product.PriceCents.Currency);
            product.ChangePrice(newPrice);
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && product.Description != request.Description)
        {
            product.ChangeDescription(request.Description);
        }

        if (request.IsActive.HasValue && product.IsActive != request.IsActive)
        {
            if(request.IsActive.Value) 
                product.Activate();
            else product.Deactivate();
        }
        
        var outboxMessages = product.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(ie => ie != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        product.ClearDomainEvents();
        ProductReadCache.Invalidate(product.Id);

        var view = ProductViewFactory.FromProduct(product);
        return ApiResponse<ProductView>.SuccessResponse(view, "Product updated successfully");
    }
}
