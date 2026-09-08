using MediatR;

namespace EventHub.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand(
    Guid Id,
    string Title,
    string Description,
    DateTime Date,
    string Location,
    Guid OrganizerId) : IRequest;
