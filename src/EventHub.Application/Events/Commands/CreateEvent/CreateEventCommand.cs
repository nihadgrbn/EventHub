using System.Text.Json.Serialization;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.CreateEvent;

public record CreateEventCommand(
    string Title,
    string Description,
    DateTime Date,
    string Location,
    EventCategory Category, 
    string Address,
    string? PosterImageUrl,
    decimal? Latitude,
    decimal? Longitude,
    List<CreateTicketTypeDto> TicketTypes
) : IRequest<Guid>, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => ["events"];
}
