using MediatR;

namespace EventHub.Application.Events.Commands.DeleteEvent;

public record DeleteEventCommand(Guid Id) : IRequest;
