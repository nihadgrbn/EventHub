using EventHub.Domain.Common;

namespace EventHub.Domain.Entities;

public sealed class Purchase : BaseEntity
{
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid AttendeeId { get; set; }
    public User? Attendee { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
