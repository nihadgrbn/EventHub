using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.Quantity).IsRequired();
        builder.Property(reservation => reservation.UnitPrice)
            .HasColumnType("decimal(18,2)");
        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(reservation => reservation.Payment)
            .WithMany(payment => payment.Reservations)
            .HasForeignKey(reservation => reservation.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(reservation => reservation.Attendee)
            .WithMany()
            .HasForeignKey(reservation => reservation.AttendeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(reservation => reservation.Event)
            .WithMany()
            .HasForeignKey(reservation => reservation.EventId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(reservation => reservation.TicketType)
            .WithMany()
            .HasForeignKey(reservation => reservation.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reservation => new
        {
            reservation.PaymentId,
            reservation.TicketTypeId
        }).IsUnique();
        builder.HasIndex(reservation => new
        {
            reservation.Status,
            reservation.ReservedUntil
        });
    }
}
