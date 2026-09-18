using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Commands.UpdateEventStatus;

public record UpdateEventStatusCommand(Guid Id, EventStatus Status) : IRequest;
