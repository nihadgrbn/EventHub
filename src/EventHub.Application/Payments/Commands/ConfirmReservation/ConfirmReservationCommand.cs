using MediatR;

namespace EventHub.Application.Payments.Commands.ConfirmReservation;

public sealed record ConfirmReservationCommand(Guid ReservationId)
    : IRequest<ConfirmReservationResult>;

public sealed record ConfirmReservationResult(
    Guid PaymentId,
    Guid ReservationId,
    Guid PurchaseId,
    IReadOnlyList<Guid> TicketIds);
