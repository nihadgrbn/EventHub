using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public record EventResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime Date,
        string Location,
        string Category,
        string Address,
        string? PosterImageUrl,
        decimal? Latitude,
        decimal? Longitude,
        EventHub.Domain.Enums.EventStatus Status,
        Guid OrganizerId,
        string OrganizerName,
        IEnumerable<TicketTypeResponseDto> TicketTypes);
    
}
