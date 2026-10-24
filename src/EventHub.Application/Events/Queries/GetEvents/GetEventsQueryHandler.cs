using EventHub.Application.Common.Interfaces;
using EventHub.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PaginatedList<EventResponse>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICacheService _cacheService;

        public GetEventsQueryHandler(IEventRepository eventRepository, ICacheService cacheService)
        {
            _eventRepository = eventRepository;
            _cacheService = cacheService;
        }

        public async Task<PaginatedList<EventResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"events:catalog:{request.SearchTerm}:{request.Location}:{request.Category}:{request.DateFrom:yyyyMMdd}:{request.DateTo:yyyyMMdd}:{request.SortBy}:{request.SortOrder}:{request.PageNumber}:{request.PageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async token =>
                {
                    var (events, totalCount) = await _eventRepository.GetPagedEventsAsync(
                        request.SearchTerm,
                        request.Location,
                        request.Category,
                        request.DateFrom,
                        request.DateTo,
                        request.SortBy,
                        request.SortOrder,
                        request.PageNumber,
                        request.PageSize,
                        token);

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
                },
                expiration: TimeSpan.FromMinutes(3),
                tags: ["events"],
                cancellationToken: cancellationToken);
        }
    }
}
