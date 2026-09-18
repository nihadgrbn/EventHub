using MediatR;

namespace EventHub.Application.Events.Commands.UpdateTicketTypes;

public record UpdateEventTicketTypesCommand(Guid Id, List<UpdateTicketTypeDto> TicketTypes) : IRequest;
