using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class StripeWebhookInboxConfiguration : IEntityTypeConfiguration<StripeWebhookInbox>
{
    public void Configure(EntityTypeBuilder<StripeWebhookInbox> builder)
    {
        builder.HasKey(webhook => webhook.Id);
        builder.Property(webhook => webhook.EventId).HasMaxLength(255).IsRequired();
        builder.Property(webhook => webhook.EventType).HasMaxLength(100).IsRequired();
        builder.Property(webhook => webhook.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(webhook => webhook.Status).HasMaxLength(20).IsRequired();
        builder.Property(webhook => webhook.LastError).HasMaxLength(2_000);
        builder.HasIndex(webhook => webhook.EventId).IsUnique();
        builder.HasIndex(webhook => new { webhook.Status, webhook.CreatedAt });
        builder.HasIndex(webhook => webhook.PaymentId);
    }
}
