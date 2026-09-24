using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface IPurchaseRepository
{
    Task AddAsync(Purchase purchase, CancellationToken cancellationToken);
    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Purchase?> GetByIdempotencyKeyAsync(
        Guid attendeeId,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
