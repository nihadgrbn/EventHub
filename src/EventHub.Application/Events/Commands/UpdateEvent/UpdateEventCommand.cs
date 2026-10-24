using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

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
    decimal? Longitude) : IRequest, IInvalidatesCache
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{Id}", "events"];
}
