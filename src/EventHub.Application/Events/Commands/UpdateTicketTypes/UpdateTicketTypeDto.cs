namespace EventHub.Application.Events.Commands.UpdateTicketTypes;

public record UpdateTicketTypeDto(Guid? Id, string Name, decimal Price, int Quantity);
