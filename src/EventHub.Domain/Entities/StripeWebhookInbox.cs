using EventHub.Domain.Common;

namespace EventHub.Domain.Entities;

public sealed class StripeWebhookInbox : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string Status { get; set; } = StripeWebhookInboxStatus.Received;
    public int RetryCount { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}

public static class StripeWebhookInboxStatus
{
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}
