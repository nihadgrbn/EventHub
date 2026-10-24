using EventHub.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.DeletePoster;

public record DeleteEventPosterCommand(Guid EventId) : IRequest, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{EventId}", "events"];
}
