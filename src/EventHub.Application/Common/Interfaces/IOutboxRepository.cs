using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<OutboxMessage>> GetBatchAsync(
        int batchSize,
        DateTime utcNow,
        CancellationToken cancellationToken);
    Task<bool> TryMarkProcessingAsync(Guid messageId, CancellationToken cancellationToken);
    Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid messageId, string error, DateTime nextAttemptAt, CancellationToken cancellationToken);
}
