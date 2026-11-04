using MediatR;

namespace EventHub.Application.Tickets.Commands.CheckInTicket;

public record CheckInTicketCommand(Guid EventId, string QrToken) : IRequest<CheckInTicketResponseDto>;

public sealed record CheckInTicketResponseDto(
    Guid TicketId,
    Guid EventId,
    string EventTitle,
    string TicketTypeName,
    decimal PriceAtPurchase,
    Guid AttendeeId,
    string AttendeeName,
    string AttendeeEmail,
    DateTime CheckedInAt);
