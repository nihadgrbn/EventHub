using MediatR;
using System.Text.Json.Serialization;
using EventHub.Domain.Enums;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand(
    string Title,
    string Description,
    DateTime Date,
    string Location,
    EventCategory Category, 
    string Address,
    string? PosterImageUrl,
    decimal? Latitude,
    decimal? Longitude) : IRequest
{
    [JsonIgnore]
    public Guid Id { get; init; }
}
