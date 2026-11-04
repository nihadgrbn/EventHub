using System;

namespace EventHub.Application.Organizers.Queries.GetOrders;

public record OrganizerOrderDto(
    Guid TicketId,
    Guid EventId,
    string EventName,
    string TicketTypeName,
    decimal Price,
    Guid AttendeeId,
    string AttendeeName,
    DateTime PurchaseDate,
    string Status,
    DateTime? CheckedInAt
);
