namespace EventHub.Application.Common.Interfaces;

public interface IStripeWebhookInboxRepository
{
    Task<bool> TryStartProcessingAsync(
        string eventId,
        string eventType,
        Guid? paymentId,
        string payloadHash,
        DateTime receivedAt,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(
        string eventId,
        DateTime processedAt,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        string eventId,
        string error,
        CancellationToken cancellationToken);
}
