using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Commands.CreateEvent
{
    public record CreateEventCommand(
        string Title,
        string Description,
        DateTime Date,
        string Location,
        string Category,
        string Address,
        string? PosterImageUrl,
        decimal? Latitude,
        decimal? Longitude,
        List<CreateTicketTypeDto> TicketTypes
        ) : IRequest<Guid>;
    
}
