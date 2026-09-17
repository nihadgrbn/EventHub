using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);
    private readonly ApplicationDbContext _context;

    public OutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetBatchAsync(
        int batchSize,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var leaseCutoff = utcNow - ProcessingLease;

        return await _context.OutboxMessages
            .Where(message =>
                (message.Status == OutboxMessageStatus.Pending ||
                 message.Status == OutboxMessageStatus.Failed ||
                 (message.Status == OutboxMessageStatus.Processing &&
                  (message.ProcessingStartedAt == null || message.ProcessingStartedAt <= leaseCutoff))) &&
                (message.NextAttemptAt == null || message.NextAttemptAt <= utcNow) &&
                message.RetryCount < 10)
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryMarkProcessingAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var leaseCutoff = DateTime.UtcNow - ProcessingLease;
        var processingStartedAt = DateTime.UtcNow;

        var affectedRows = await _context.OutboxMessages
            .Where(message => message.Id == messageId &&
                              (message.Status == OutboxMessageStatus.Pending ||
                               message.Status == OutboxMessageStatus.Failed ||
                               (message.Status == OutboxMessageStatus.Processing &&
                                (message.ProcessingStartedAt == null || message.ProcessingStartedAt <= leaseCutoff))))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                .SetProperty(message => message.ProcessingStartedAt, processingStartedAt), cancellationToken);

        return affectedRows == 1;
    }

    public async Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await _context.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Sent)
                .SetProperty(message => message.ProcessedAt, DateTime.UtcNow)
                .SetProperty(message => message.ProcessingStartedAt, (DateTime?)null), cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTime nextAttemptAt,
        CancellationToken cancellationToken)
    {
        await _context.OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Failed)
                .SetProperty(message => message.RetryCount, message => message.RetryCount + 1)
                .SetProperty(message => message.LastError, error)
                .SetProperty(message => message.NextAttemptAt, nextAttemptAt)
                .SetProperty(message => message.ProcessingStartedAt, (DateTime?)null), cancellationToken);
    }
}
