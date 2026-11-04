using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Payments.Commands.CancelReservation;

public sealed class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReservationRepository _reservations;
    private readonly IPaymentRepository _payments;
    private readonly ITicketTypeRepository _ticketTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICacheService? _cacheService;

    public CancelReservationCommandHandler(
        ICurrentUserService currentUser,
        IReservationRepository reservations,
        IPaymentRepository payments,
        ITicketTypeRepository ticketTypes,
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        ICacheService? cacheService = null)
    {
        _currentUser = currentUser;
        _reservations = reservations;
        _payments = payments;
        _ticketTypes = ticketTypes;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _cacheService = cacheService;
    }

    public async Task Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var attendeeId = _currentUser.UserId
            ?? throw new UnauthorizedException("You must be logged in to cancel a reservation.");
        var reservation = await _reservations.GetForConfirmationAsync(
            request.ReservationId,
            attendeeId,
            cancellationToken)
            ?? throw new NotFoundException("Reservation not found.");

        if (reservation.Payment is null
            || reservation.Status != ReservationStatus.Active
            || reservation.Payment.Status != PaymentStatus.Pending)
        {
            throw new ConflictException("Only an active pending reservation can be cancelled.");
        }

        if (!string.IsNullOrWhiteSpace(reservation.Payment.StripeCheckoutSessionId))
        {
            await _paymentGateway.ExpireCheckoutSessionAsync(
                reservation.Payment.StripeCheckoutSessionId,
                cancellationToken);
        }

        var cancelledAt = DateTime.UtcNow;
        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            if (!await _payments.TryMarkAsFailedAsync(
                    reservation.PaymentId,
                    cancelledAt,
                    transactionCancellationToken))
            {
                throw new ConflictException("The reservation is no longer pending.");
            }

            if (!await _ticketTypes.TryIncreaseAvailableQuantityAsync(
                    reservation.TicketTypeId,
                    reservation.Quantity,
                    transactionCancellationToken))
            {
                throw new InvalidOperationException(
                    $"Unable to restore inventory for reservation {reservation.Id}.");
            }

            await _reservations.UpdateStatusAsync(
                reservation.Id,
                ReservationStatus.Cancelled,
                cancelledAt,
                transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        if (_cacheService is not null)
        {
            await _cacheService.RemoveByTagAsync($"event:{reservation.EventId}", cancellationToken);
            await _cacheService.RemoveByTagAsync("events", cancellationToken);
        }
    }
}
