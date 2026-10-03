using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Models;
using EventHub.Application.Events.Queries.GetEvents;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventHub.Application.Events.Queries.GetOrganizerEvents
{
    public class GetOrganizerEventsQueryHandler : IRequestHandler<GetOrganizerEventsQuery, PaginatedList<EventResponse>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetOrganizerEventsQueryHandler(IEventRepository eventRepository, ICurrentUserService currentUserService)
        {
            _eventRepository = eventRepository;
            _currentUserService = currentUserService;
        }

        public async Task<PaginatedList<EventResponse>> Handle(GetOrganizerEventsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId
                ?? throw new UnauthorizedException("A valid user is required to view organizer events.");

            var (events, totalCount) = await _eventRepository.GetOrganizerEventsAsync(
                userId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var eventResponses = events.Select(@event => new EventResponse(
                @event.Id,
                @event.Title,
                @event.Description,
                @event.Date,
                @event.Location,
                @event.Category,
                @event.Address,
                @event.PosterImageUrl,
                @event.Latitude,
                @event.Longitude,
                @event.Status,
                @event.RejectionReason,
                @event.OrganizerId,
                @event.Organizer is null ? string.Empty : $"{@event.Organizer.FirstName} {@event.Organizer.LastName}",
                @event.TicketTypes.Select(t => new TicketTypeResponseDto(
                    t.Id, t.Name, t.Price, t.AvailableQuantity
                ))
            )).ToList();

            return new PaginatedList<EventResponse>(
                eventResponses, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
