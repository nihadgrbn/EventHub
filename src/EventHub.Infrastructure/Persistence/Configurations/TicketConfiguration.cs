using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasOne(t => t.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Attendee)
            .WithMany(u => u.PurchasedTickets)
            .HasForeignKey(t => t.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Purchase)
            .WithMany(p => p.Tickets)
            .HasForeignKey(t => t.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.TicketTypeNameAtPurchase)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.PriceAtPurchase)
            .HasColumnType("decimal(18,2)");

        builder.Property(t => t.QrTokenHash).HasMaxLength(64);
        builder.HasIndex(t => t.QrTokenHash).IsUnique();

        builder.HasOne(t => t.TicketType)
            .WithMany(tt => tt.Tickets)
            .HasForeignKey(t => t.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
