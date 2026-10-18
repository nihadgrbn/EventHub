using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface IStripeEventRepository
{
    Task<bool> TryMarkAsProcessedAsync(
        string eventId,
        CancellationToken cancellationToken);
}
