using EventHub.Domain.Common;

namespace EventHub.Domain.Entities;

public sealed class ProcessedStripeEvent : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
}
