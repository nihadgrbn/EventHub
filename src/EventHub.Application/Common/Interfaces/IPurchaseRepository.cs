using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase, CancellationToken cancellationToken);
}
