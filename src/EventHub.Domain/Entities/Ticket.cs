using EventHub.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Domain.Entities
{
    public class Ticket : BaseEntity
    {
        public Guid EventId { get; set; }
        public Event? Event { get; set; }
        public Guid PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }
        public Guid TicketTypeId {  get; set; }
        public TicketType? TicketType { get; set; }
        public string TicketTypeNameAtPurchase { get; set; } = string.Empty;
        public decimal PriceAtPurchase { get; set; }
        public Guid AttendeeId { get; set; } 
        public User? Attendee { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
