using EventHub.Domain.Common;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public Guid AttendeeId { get; set; }
    public User? Attendee { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? IdempotencyKey { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
