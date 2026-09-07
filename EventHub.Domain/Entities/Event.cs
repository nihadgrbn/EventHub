using EventHub.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Domain.Entities
{
    public class Event : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; }= string.Empty;
        public DateTime Date {  get; set; }
        public string Location { get; set; }

        public Guid OrganizerId {  get; set; }
        public User? Organizer { get; set; }

        public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();

    }
}
