using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
    {
        builder.HasKey(stripeEvent => stripeEvent.Id);
        builder.Property(stripeEvent => stripeEvent.EventId)
            .HasMaxLength(255)
            .IsRequired();
        builder.HasIndex(stripeEvent => stripeEvent.EventId)
            .IsUnique();
    }
}
