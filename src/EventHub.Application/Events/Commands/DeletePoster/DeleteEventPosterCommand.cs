using MediatR;

namespace EventHub.Application.Events.Commands.DeletePoster;

public record DeleteEventPosterCommand(Guid EventId) : IRequest;
