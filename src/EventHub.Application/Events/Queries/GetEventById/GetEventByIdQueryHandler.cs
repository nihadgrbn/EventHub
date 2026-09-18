using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Events.Queries.GetEvents;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventResponse>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetEventByIdQueryHandler(IEventRepository eventRepository, ICurrentUserService currentUserService)
        {
            _eventRepository = eventRepository;
            _currentUserService = currentUserService;
        }

        public async Task<EventResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetByIdAsync(request.Id, cancellationToken);

            if (@event is null)
                throw new NotFoundException("Event not found.");

            if (@event.Status != EventHub.Domain.Enums.EventStatus.Published
                && !_currentUserService.IsInRole(EventHub.Domain.Constants.Roles.Admin)
                && @event.OrganizerId != _currentUserService.UserId)
            {
                throw new NotFoundException("Event not found.");
            }

            return new EventResponse(
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
}
