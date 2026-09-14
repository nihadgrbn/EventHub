using EventHub.Domain.Common;

namespace EventHub.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public string Role {  get; set; } = string.Empty;
        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Ticket> PurchasedTickets { get; set; } = new List<Ticket>();
    }
}
