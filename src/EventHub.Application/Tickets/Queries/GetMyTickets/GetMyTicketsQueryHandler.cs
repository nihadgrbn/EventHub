using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventHub.Application.Tickets.Queries.GetMyTickets
{
    public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, IEnumerable<MyTicketResponseDto>>
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetMyTicketsQueryHandler(ITicketRepository ticketRepository, ICurrentUserService currentUserService)
        {
            _ticketRepository = ticketRepository;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<MyTicketResponseDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("You must be logged in to view your tickets.");

            var tickets = await _ticketRepository.GetTicketsByUserIdAsync(userId, cancellationToken);

            return tickets.Select(t => new MyTicketResponseDto(
                t.Id,
                t.EventId,
                t.Event!.Title,
                t.Event.Date,
                t.Event.Location,
                t.Event.Status.ToString(),
                !string.IsNullOrWhiteSpace(t.TicketTypeNameAtPurchase) ? t.TicketTypeNameAtPurchase : t.TicketType!.Name,
                t.PriceAtPurchase > 0m || t.TicketType is null ? t.PriceAtPurchase : t.TicketType.Price,
                t.PurchaseDate,
                t.Status.ToString(),
                t.CheckedInAt
            ));
        }
    }
}
