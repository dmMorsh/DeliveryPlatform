using MediatR;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using CatalogService.Domain.ValueObjects;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductView>>
{
    private readonly IProductRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateProductCommandHandler(IProductRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<ProductView>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.ProductId, ct);

        if (product == null)
            return ApiResponse<ProductView>.ErrorResponse("Product not found");

        if (request.PriceCents.HasValue)
        {
            var newPrice = new Money(request.PriceCents.Value, "USD");
            product.ChangePrice(newPrice);
        }

        // Note: Product aggregate doesn't expose Name/Description update directly
        // This is a limitation of the current design - consider extending Product aggregate

        await _repo.UpdateAsync(product, ct);

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

        var view = new ProductView(product.Id, product.Name, "", (long)product.Price.Amount, 0, DateTime.UtcNow);
        return ApiResponse<ProductView>.SuccessResponse(view, "Product updated successfully");
    }
}
