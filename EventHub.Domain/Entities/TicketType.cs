using EventHub.Domain.Common;
using System.Net.Sockets;

namespace EventHub.Domain.Entities
{
    public class TicketType : BaseEntity
    {
        public string Name { get; set; } = string.Empty; //vip,standart, and etc
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        
        public Guid EventId { get; set; }
        public Event? Event { get; set; }
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    }
}
