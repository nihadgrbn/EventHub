using EventHub.Domain.Common;

namespace EventHub.Domain.Entities;

public sealed class Purchase : BaseEntity
{
    public Guid AttendeeId { get; set; }
    public User? Attendee { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
