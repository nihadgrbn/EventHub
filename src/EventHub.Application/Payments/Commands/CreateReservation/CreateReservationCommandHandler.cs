using EventHub.Application.Common.Exceptions;
using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Payments.Commands.CreateReservation;

public sealed class CreateReservationCommandHandler
    : IRequestHandler<CreateReservationCommand, CreateReservationResult>
{
    private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(15);

    private readonly ITicketTypeRepository _ticketTypes;
    private readonly IEventRepository _events;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentRepository _payments;
    private readonly IReservationRepository _reservations;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReservationCommandHandler(
        ITicketTypeRepository ticketTypes,
        IEventRepository events,
        ICurrentUserService currentUser,
        IPaymentRepository payments,
        IReservationRepository reservations,
        IUnitOfWork unitOfWork)
    {
        _ticketTypes = ticketTypes;
        _events = events;
        _currentUser = currentUser;
        _payments = payments;
        _reservations = reservations;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateReservationResult> Handle(
        CreateReservationCommand request,
        CancellationToken cancellationToken)
    {
        var attendeeId = _currentUser.UserId
            ?? throw new UnauthorizedException("You must be logged in to reserve tickets.");

        if (request.Quantity <= 0)
        {
            throw new ConflictException("Quantity must be greater than zero.");
        }

        var idempotencyKey = request.IdempotencyKey?.Trim();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingPayment = await _payments.GetByIdempotencyKeyAsync(
                attendeeId,
                idempotencyKey,
                cancellationToken);

            if (existingPayment is not null)
            {
                var existingReservation = existingPayment.Reservations.SingleOrDefault();
                if (existingReservation is null
                    || existingReservation.EventId != request.EventId
                    || existingReservation.TicketTypeId != request.TicketTypeId
                    || existingReservation.Quantity != request.Quantity)
                {
                    throw new ConflictException(
                        "The idempotency key was already used for a different reservation request.");
                }

                return new CreateReservationResult(
                    existingPayment.Id,
                    existingReservation.Id,
                    existingPayment.Amount,
                    existingPayment.Currency,
                    existingPayment.ExpiresAt);
            }
        }

        var ticketType = await _ticketTypes.GetByIdAsync(request.TicketTypeId, cancellationToken)
            ?? throw new NotFoundException("Ticket type not found.");

        if (ticketType.EventId != request.EventId)
        {
            throw new ConflictException("The ticket type does not belong to the selected event.");
        }

        var eventEntity = await _events.GetByIdAsync(request.EventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        if (eventEntity.Status != EventStatus.Published)
        {
            throw new ConflictException("Tickets can only be reserved for published events.");
        }

        var expiresAt = DateTime.UtcNow.Add(ReservationDuration);
        var payment = new Payment
        {
            AttendeeId = attendeeId,
            Amount = ticketType.Price * request.Quantity,
            Currency = "AZN",
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            ExpiresAt = expiresAt
        };
        var reservation = new Reservation
        {
            Payment = payment,
            AttendeeId = attendeeId,
            EventId = request.EventId,
            TicketTypeId = request.TicketTypeId,
            Quantity = request.Quantity,
            UnitPrice = ticketType.Price,
            ReservedUntil = expiresAt,
            Status = ReservationStatus.Active
        };

        await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            if (!await _ticketTypes.TryDecreaseAvailableQuantityAsync(
                    request.TicketTypeId,
                    request.Quantity,
                    transactionCancellationToken))
            {
                throw new ConflictException("There are not enough tickets available.");
            }

            await _payments.AddAsync(payment, transactionCancellationToken);
            await _reservations.AddAsync(reservation, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return new CreateReservationResult(
            payment.Id,
            reservation.Id,
            payment.Amount,
            payment.Currency,
            payment.ExpiresAt);
    }
}
