using MediatR;

namespace EventHub.Application.Payments.Commands.CreateReservation;

public sealed record CreateReservationCommand(
    Guid EventId,
    Guid TicketTypeId,
    int Quantity,
    string? IdempotencyKey = null) : IRequest<CreateReservationResult>;

public sealed record CreateReservationResult(
    Guid PaymentId,
    Guid ReservationId,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt);
