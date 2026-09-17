using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Status).HasMaxLength(30).IsRequired();
        builder.Property(message => message.IdempotencyKey).HasMaxLength(200);
        builder.HasIndex(message => new { message.Status, message.NextAttemptAt });
        builder.HasIndex(message => message.IdempotencyKey).IsUnique();
    }
}