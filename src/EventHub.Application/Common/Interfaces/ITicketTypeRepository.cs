using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface ITicketTypeRepository
{
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> TryDecreaseAvailableQuantityAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken);
    Task<bool> TryIncreaseAvailableQuantityAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken);
}