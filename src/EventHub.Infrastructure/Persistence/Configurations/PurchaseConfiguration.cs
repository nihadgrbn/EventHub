using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.HasKey(purchase => purchase.Id);
        builder.Property(purchase => purchase.IdempotencyKey).HasMaxLength(100);
        builder.Property(purchase => purchase.TotalAmount).HasColumnType("decimal(18,2)");
        builder.HasIndex(purchase => purchase.AttendeeId);
        builder.HasIndex(purchase => new { purchase.AttendeeId, purchase.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}