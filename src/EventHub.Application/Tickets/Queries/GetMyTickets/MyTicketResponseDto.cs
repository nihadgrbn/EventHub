using System;

namespace EventHub.Application.Tickets.Queries.GetMyTickets
{
    public record MyTicketResponseDto(
        Guid TicketId,
        string EventTitle,
        DateTime EventDate,
        string Location,
        string TicketTypeName,
        decimal Price,
        DateTime PurchaseDate
    );
}