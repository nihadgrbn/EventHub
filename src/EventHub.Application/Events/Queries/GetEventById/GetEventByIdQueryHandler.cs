using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Queries.GetEvents;
using EventHub.Domain.Constants;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponse>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public GetEventByIdQueryHandler(
            IEventRepository eventRepository,
            ICurrentUserService currentUserService,
            ICacheService cacheService)
        {
            _eventRepository = eventRepository;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        public async Task<EventResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var isPrivileged = _currentUserService.IsInRole(Roles.Admin);

            if (!isPrivileged)
            {
                var cached = await _cacheService.GetOrCreateAsync(
                    $"events:{request.Id}",
                    async token =>
                    {
                        var entity = await _eventRepository.GetByIdAsync(request.Id, token);
                        if (entity is null || entity.Status != EventStatus.Published)
                        {
                            return null;
                        }

                        return MapToResponse(entity);
                    },
                    expiration: TimeSpan.FromMinutes(10),
                    tags: [$"event:{request.Id}", "events"],
                    cancellationToken: cancellationToken);

                if (cached is not null)
                {
                    return cached;
                }
            }

            var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

            if (@event is null)
                throw new NotFoundException("Event not found.");

            if (@event.Status != EventStatus.Published
                && !_currentUserService.IsInRole(Roles.Admin)
                && @event.OrganizerId != _currentUserService.UserId)
            {
                throw new NotFoundException("Event not found.");
            }

            return MapToResponse(@event);
        }

        private static EventResponse MapToResponse(Domain.Entities.Event @event) =>
            new(
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
                @event.Organizer is null
                    ? string.Empty
                    : $"{@event.Organizer.FirstName} {@event.Organizer.LastName}",
                @event.TicketTypes.Select(ticketType => new TicketTypeResponseDto(
                    ticketType.Id,
                    ticketType.Name,
                    ticketType.Price,
                    ticketType.AvailableQuantity
                )).ToList());
    }
}
