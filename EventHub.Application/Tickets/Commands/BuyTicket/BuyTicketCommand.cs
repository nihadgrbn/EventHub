using MediatR;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed record BuyTicketCommand(Guid EventId, Guid TicketTypeId) : IRequest<Guid>;
