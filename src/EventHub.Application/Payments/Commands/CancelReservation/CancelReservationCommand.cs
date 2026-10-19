using MediatR;

namespace EventHub.Application.Payments.Commands.CancelReservation;

public sealed record CancelReservationCommand(Guid ReservationId) : IRequest;
