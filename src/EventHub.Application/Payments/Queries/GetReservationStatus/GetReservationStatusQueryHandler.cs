using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Payments.Queries.GetReservationStatus;

public sealed class GetReservationStatusQueryHandler
    : IRequestHandler<GetReservationStatusQuery, ReservationStatusResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReservationRepository _reservations;

    public GetReservationStatusQueryHandler(
        ICurrentUserService currentUser,
        IReservationRepository reservations)
    {
        _currentUser = currentUser;
        _reservations = reservations;
    }

    public async Task<ReservationStatusResponse> Handle(
        GetReservationStatusQuery request,
        CancellationToken cancellationToken)
    {
        var attendeeId = _currentUser.UserId
            ?? throw new UnauthorizedException("You must be logged in to view reservation status.");

        var reservation = await _reservations.GetForConfirmationAsync(
            request.ReservationId,
            attendeeId,
            cancellationToken)
            ?? throw new NotFoundException("Reservation not found.");

        if (reservation.Payment is null)
        {
            throw new ConflictException("Reservation payment data is incomplete.");
        }

        return new ReservationStatusResponse(
            reservation.Id,
            reservation.PaymentId,
            reservation.Payment.Status.ToString(),
            reservation.Status.ToString(),
            reservation.Payment.Amount,
            reservation.Payment.Currency,
            reservation.Payment.ExpiresAt,
            reservation.Payment.StripeCheckoutUrl);
    }
}
