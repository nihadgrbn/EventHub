using EventHub.Domain.Common;

namespace EventHub.Domain.Entities;

public sealed class OutboxMessage : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = OutboxMessageStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
    public string? IdempotencyKey { get; set; }
}

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}
