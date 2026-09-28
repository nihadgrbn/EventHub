using MediatR;

namespace EventHub.Application.Events.Commands.UploadPoster;

public record UploadEventPosterCommand(
    Guid EventId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Length) : IRequest<string>;
