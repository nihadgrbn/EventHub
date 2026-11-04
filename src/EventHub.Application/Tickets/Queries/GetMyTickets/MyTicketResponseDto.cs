using System;

namespace EventHub.Application.Tickets.Queries.GetMyTickets
{
    public record MyTicketResponseDto(
        Guid TicketId,
        Guid EventId,
        string EventTitle,
        DateTime EventDate,
        string Location,
        string EventStatus,
        string TicketTypeName,
        decimal Price,
        DateTime PurchaseDate,
        string Status,
        DateTime? CheckedInAt
    );
}
