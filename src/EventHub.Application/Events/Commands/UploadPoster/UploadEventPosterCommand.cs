using EventHub.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace EventHub.Application.Events.Commands.UploadPoster;

public record UploadEventPosterCommand(
    Guid EventId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Length) : IRequest<string>, IInvalidatesCache
{
    [JsonIgnore]
    public IReadOnlyCollection<string> CacheTags => [$"event:{EventId}", "events"];
}
