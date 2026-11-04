using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Models;
using MediatR;

namespace EventHub.Application.Organizers.Queries.GetOrders;

public class GetOrganizerOrdersQueryHandler : IRequestHandler<GetOrganizerOrdersQuery, PaginatedList<OrganizerOrderDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizerOrdersQueryHandler(ITicketRepository ticketRepository, ICurrentUserService currentUserService)
    {
        _ticketRepository = ticketRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<OrganizerOrderDto>> Handle(GetOrganizerOrdersQuery request, CancellationToken cancellationToken)
    {
        var organizerId = _currentUserService.UserId;
        if (organizerId is null)
        {
            throw new UnauthorizedException("You are not logged in.You must be logged in to view your orders.");
        }

        var scope = _currentUserService.IsInRole(EventHub.Domain.Constants.Roles.Admin)
            ? (Guid?)null
            : organizerId;

        var (tickets, totalCount) = await _ticketRepository.GetOrdersByOrganizerIdAsync(
            scope, request.PageNumber, request.PageSize, cancellationToken);

        var orderDtos = tickets.Select(t => new OrganizerOrderDto(
            t.Id,
            t.EventId,
            t.Event!.Title,
            !string.IsNullOrWhiteSpace(t.TicketTypeNameAtPurchase) ? t.TicketTypeNameAtPurchase : t.TicketType!.Name,
            t.PriceAtPurchase > 0m || t.TicketType is null ? t.PriceAtPurchase : t.TicketType.Price,
            t.AttendeeId,
            t.Attendee != null ? $"{t.Attendee.FirstName} {t.Attendee.LastName}" : "Unknown Attendee",
            t.PurchaseDate,
            t.Status.ToString(),
            t.CheckedInAt
        )).ToList();

        return new PaginatedList<OrganizerOrderDto>(orderDtos, totalCount, request.PageNumber, request.PageSize);
    }
}
