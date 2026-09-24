using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount)
            .HasColumnType("decimal(18,2)");
        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(payment => payment.StripeCheckoutSessionId)
            .HasMaxLength(255);
        builder.Property(payment => payment.StripePaymentIntentId)
            .HasMaxLength(255);
        builder.Property(payment => payment.IdempotencyKey)
            .HasMaxLength(100);

        builder.HasOne(payment => payment.Attendee)
            .WithMany()
            .HasForeignKey(payment => payment.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(payment => new { payment.AttendeeId, payment.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(payment => payment.StripeCheckoutSessionId)
            .IsUnique()
            .HasFilter("\"StripeCheckoutSessionId\" IS NOT NULL");
        builder.HasIndex(payment => payment.Status);
        builder.HasIndex(payment => payment.ExpiresAt);
    }
}
