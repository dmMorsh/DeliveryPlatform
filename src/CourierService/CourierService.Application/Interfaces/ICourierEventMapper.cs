using CourierService.Domain.SeedWork;
using Shared.Contracts.Events;

namespace CourierService.Application.Interfaces;

public interface ICourierEventMapper
{
    IntegrationEvent? MapFromDomainEvent(DomainEvent domainEvent);
}