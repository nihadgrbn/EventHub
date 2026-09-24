using MediatR;

namespace EventHub.Application.Tickets.Commands.BuyTicket;

public sealed record BuyTicketCommand(
    Guid EventId,
    Guid TicketTypeId,
    int Quantity,
    string? IdempotencyKey = null) : IRequest<IReadOnlyList<Guid>>;
