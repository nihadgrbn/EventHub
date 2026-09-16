using MediatR;
using System.Collections.Generic;

namespace EventHub.Application.Tickets.Queries.GetMyTickets
{
    public record GetMyTicketsQuery() : IRequest<IEnumerable<MyTicketResponseDto>>;
}