using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using CatalogService.Domain.Events;
using CatalogService.Domain.ValueObjects;
using MediatR;
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

        var model = request.UpdateProductModel;
        
        if (model.PriceCents.HasValue && product.PriceCents.AmountCents != model.PriceCents)
        {
            var newPrice = new Money(model.PriceCents.Value, model.Currency ?? product.PriceCents.Currency);
            product.ChangePrice(newPrice);
        }

        if (!string.IsNullOrWhiteSpace(model.Description) && product.Description != model.Description)
        {
            product.ChangeDescription(model.Description);
        }

        if (model.IsActive.HasValue && product.IsActive != model.IsActive)
        {
            if(model.IsActive.Value) 
                product.Activate();
            else product.Deactivate();
        }
        
        await _repo.UpdateAsync(product, ct);

        var outboxMessages = new List<OutboxMessage>();

        foreach (var domainEvent in product.DomainEvents)
        {
            if (domainEvent is ProductPriceChanged priceChangedEvent)
            {
                var integrationEvent = _eventMapper.MapProductPriceChangedEvent(
                    product.Id, 
                    priceChangedEvent.OldPrice.AmountCents, 
                    priceChangedEvent.NewPrice.AmountCents);
                outboxMessages.Add(OutboxMessage.From(integrationEvent));
            }
        }

        await _uow.SaveChangesAsync(outboxMessages, ct);
        product.ClearDomainEvents();

        var view = new ProductView(product.Id, product.Name, product.Description, product.PriceCents.AmountCents, product.PriceCents.Currency, product.WeightGrams.Value);
        return ApiResponse<ProductView>.SuccessResponse(view, "Product updated successfully");
    }
}
