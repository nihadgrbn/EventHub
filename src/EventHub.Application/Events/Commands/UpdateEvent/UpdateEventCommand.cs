using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand(
    string Title,
    string Description,
    DateTime Date,
    string Location) : IRequest
{
    [JsonIgnore]
    public Guid Id { get; init; }
}
