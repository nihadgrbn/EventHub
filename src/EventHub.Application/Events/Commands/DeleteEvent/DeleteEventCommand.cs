using EventHub.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.DeleteEvent;

public record DeleteEventCommand(Guid Id) : IRequest, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{Id}", "events"];
}
