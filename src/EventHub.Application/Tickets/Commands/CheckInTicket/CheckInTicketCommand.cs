using MediatR;

namespace EventHub.Application.Tickets.Commands.CheckInTicket;

public record CheckInTicketCommand(Guid EventId, string QrToken) : IRequest;
