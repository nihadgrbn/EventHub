using EventHub.Domain.Common;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public sealed class Reservation : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid AttendeeId { get; set; }
    public User? Attendee { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid TicketTypeId { get; set; }
    public TicketType? TicketType { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime ReservedUntil { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}
