using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Models; 
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PaginatedList<EventResponse>>
    {
        private readonly IEventRepository _eventRepository;

        public GetEventsQueryHandler(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<PaginatedList<EventResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            var (events, totalCount) = await _eventRepository.GetPagedEventsAsync(
                request.SearchTerm,
                request.Location,
                request.SortBy,
                request.SortOrder,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var eventResponses = events.Select(@event => new EventResponse(
                @event.Id,
                @event.Title,
                @event.Description,
                @event.Date,
                @event.Location,
                @event.Status,
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
