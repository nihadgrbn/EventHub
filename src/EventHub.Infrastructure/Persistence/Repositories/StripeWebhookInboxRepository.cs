using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class StripeWebhookInboxRepository : IStripeWebhookInboxRepository
{
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);
    private readonly ApplicationDbContext _context;

    public StripeWebhookInboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryStartProcessingAsync(
        string eventId,
        string eventType,
        Guid? paymentId,
        string payloadHash,
        DateTime receivedAt,
        CancellationToken cancellationToken)
    {
        var webhook = await _context.StripeWebhookInboxes.SingleOrDefaultAsync(
            item => item.EventId == eventId,
            cancellationToken);

        if (webhook is null)
        {
            _context.StripeWebhookInboxes.Add(new StripeWebhookInbox
            {
                EventId = eventId,
                EventType = eventType,
                PaymentId = paymentId,
                PayloadHash = payloadHash,
                Status = StripeWebhookInboxStatus.Processing,
                RetryCount = 0,
                ProcessingStartedAt = receivedAt
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                return false;
            }
        }

        if (webhook.Status == StripeWebhookInboxStatus.Processed
            || (webhook.Status == StripeWebhookInboxStatus.Processing
                && webhook.ProcessingStartedAt > receivedAt.Subtract(ProcessingLease)))
        {
            return false;
        }

        webhook.Status = StripeWebhookInboxStatus.Processing;
        webhook.ProcessingStartedAt = receivedAt;
        webhook.RetryCount++;
        webhook.LastError = null;
        webhook.PaymentId ??= paymentId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkProcessedAsync(
        string eventId,
        DateTime processedAt,
        CancellationToken cancellationToken)
    {
        await _context.StripeWebhookInboxes
            .Where(item => item.EventId == eventId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, StripeWebhookInboxStatus.Processed)
                .SetProperty(item => item.ProcessedAt, processedAt)
                .SetProperty(item => item.LastError, (string?)null), cancellationToken);
    }

    public async Task MarkFailedAsync(
        string eventId,
        string error,
        CancellationToken cancellationToken)
    {
        var truncatedError = error[..Math.Min(error.Length, 2_000)];
        await _context.StripeWebhookInboxes
            .Where(item => item.EventId == eventId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, StripeWebhookInboxStatus.Failed)
                .SetProperty(item => item.LastError, truncatedError), cancellationToken);
    }
}
