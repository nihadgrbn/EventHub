using System;

namespace EventHub.Application.Organizers.Queries.GetOrders;

public record OrganizerOrderDto(
    Guid TicketId,
    string EventName,
    string TicketTypeName,
    decimal Price,
    Guid AttendeeId,
    string AttendeeName,
    DateTime PurchaseDate

);